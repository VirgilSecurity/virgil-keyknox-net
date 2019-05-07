namespace Keyknox.Tests
{
    using System;
    using System.Collections.Generic;
    using Keyknox.Utils;
    using Newtonsoft.Json.Linq;
    using Virgil.SDK.Common;
    using Xunit;

    public class EntrySerializerTest
    {
        private NewtonsoftJsonExtendedSerializer serializer;
        private CloudSerializer cloudSerializer;

        public EntrySerializerTest()
        {
            this.serializer = new NewtonsoftJsonExtendedSerializer();
            this.cloudSerializer = new CloudSerializer(this.serializer);
        }

        [Fact]
        public void KTC_17()
        { 
            var text = System.IO.File.ReadAllText("Cloud.json");
            var cloudTestData = this.serializer.Deserialize<Dictionary<string, dynamic>>(text);

            var cloudEntry = this.ParseEntry(cloudTestData, 1);
            var cloudEntry2 = this.ParseEntry(cloudTestData, 2);
            var entries = new Dictionary<string, CloudEntry>()
            {
                { cloudEntry.Name, cloudEntry },
                { cloudEntry2.Name, cloudEntry2 }
            };
            var serialized = this.cloudSerializer.Serialize(entries);

            var expectedResultBytes = Bytes.FromString(cloudTestData["kExpectedResult"], StringEncoding.BASE64);
            var deserialized = this.cloudSerializer.Deserialize(expectedResultBytes);

            Assert.Equal(entries.Count, deserialized.Count);

            foreach (var entry in entries)
            {
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
            var entriesList = this.cloudSerializer.Deserialize(new byte[0]);
            Assert.NotNull(entriesList);
            Assert.Empty(entriesList);
            entriesList = this.cloudSerializer.Deserialize(null);

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
                CreationDate = UnixTimestampConverterInMilliseconds.DateTimeFrom(dict[$"kCreationDate{number}"]),
                ModificationDate = UnixTimestampConverterInMilliseconds.DateTimeFrom(dict[$"kModificationDate{number}"])
            };
        }
    }
}
