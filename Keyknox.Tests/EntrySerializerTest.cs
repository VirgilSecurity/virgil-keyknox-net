using System;
using System.Collections.Generic;
using Keyknox.Utils;
using Newtonsoft.Json.Linq;
using Virgil.SDK.Common;
using Xunit;

namespace Keyknox.Tests
{
    public class EntrySerializerTest
    {
        NewtonsoftJsonExtendedSerializer serializer;
        CloudSerializer cloudSerializer;
        public EntrySerializerTest()
        {
            this.serializer = new NewtonsoftJsonExtendedSerializer();
            this.cloudSerializer = new CloudSerializer(this.serializer);
        }

        [Fact]
        public void KTC_17()
        { 
            var text = System.IO.File.ReadAllText("Cloud.json");
            var cloudTestData = serializer.Deserialize<Dictionary<string, dynamic>>(text);

            var cloudEntry = ParseEntry(cloudTestData, 1);
            var cloudEntry2 = ParseEntry(cloudTestData, 2);
            var entries = new Dictionary<string, CloudEntry>() {
                { cloudEntry.Name, cloudEntry },
                { cloudEntry2.Name, cloudEntry2 } };
            var serialized = cloudSerializer.Serialize(entries);

            var expectedResultBytes = Bytes.FromString(cloudTestData["kExpectedResult"], StringEncoding.BASE64);
            var deserialized = cloudSerializer.Deserialize(expectedResultBytes);

            Assert.Equal(entries.Count, deserialized.Count);

            foreach (var entry in entries){
                var deserializedEntry = deserialized[entry.Key];
                Assert.Equal(entry.Value.CreationDate, deserializedEntry.CreationDate);
                Assert.Equal(entry.Value.Data, deserializedEntry.Data);
                Assert.Equal(entry.Value.ModificationDate, deserializedEntry.ModificationDate);
                Assert.Equal(entry.Value.Meta, deserializedEntry.Meta);
            }
        }

        [Fact]
        public void KTC_18()
        {
            var entriesList = cloudSerializer.Deserialize(new byte[0]);
            Assert.NotNull(entriesList);
            Assert.Empty(entriesList);
            entriesList = cloudSerializer.Deserialize(null);

            Assert.NotNull(entriesList);
            Assert.Empty(entriesList);
        }

        private CloudEntry ParseEntry(Dictionary<string, dynamic> dict, int number)
        {
            var meta = ((JObject)dict[$"kMeta{number}"])?.ToObject<Dictionary<string, string>>();
            return new CloudEntry()
            {
                Name = dict[$"kName{number}"],
                Data = Bytes.FromString(dict[$"kData{number}"], StringEncoding.BASE64),
                Meta = meta,
                CreationDate = UnixTimestampConverterInMilliseconds.TimeFromUnixTimestampInMilliseconds(dict[$"kCreationDate{number}"]),
                ModificationDate = UnixTimestampConverterInMilliseconds.TimeFromUnixTimestampInMilliseconds(dict[$"kModificationDate{number}"])
            };
        }
    }
}
