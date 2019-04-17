using System;
using System.Collections.Generic;
using Keyknox.Utils;
using Virgil.SDK.Common;
using Xunit;

namespace Keyknox.Tests
{
    public class EntrySerializerTest
    {
        NewtonsoftJsonSerializer serializer;
        CloudSerializer cloudSerializer;
        public EntrySerializerTest()
        {
            this.serializer = new NewtonsoftJsonSerializer();
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
            Assert.Equal(
                cloudTestData["kExpectedResult"],
                Bytes.ToString(serialized, StringEncoding.BASE64));

            var expectedResultBytes = Bytes.FromString(cloudTestData["kExpectedResult"], StringEncoding.BASE64);
            var deserialized = cloudSerializer.Deserialize(expectedResultBytes);
            Assert.Equal(entries, deserialized);
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
            return new CloudEntry()
            {
                Name = dict[$"kName{number}"],
                Data = Bytes.FromString(dict[$"kData{number}"], StringEncoding.BASE64),
                Meta = dict[$"kMeta{number}"],
                CreationDate = ParseDateTime(dict[$"kCreationDate{number}"]),
                ModificationDate = ParseDateTime(dict[$"kModificationDate{number}"])
            };
        }

        private DateTime ParseDateTime(Int64 str)
        {
            return DateTimeOffset.FromUnixTimeSeconds(str).DateTime;
        }

    }
}
