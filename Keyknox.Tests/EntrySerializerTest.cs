using System;
using System.Collections.Generic;
using Virgil.SDK.Common;
using Xunit;

namespace Keyknox.Tests
{
    public class EntrySerializerTest
    {
        NewtonsoftJsonSerializer serializer = new NewtonsoftJsonSerializer();
        public EntrySerializerTest()
        {
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
            var serialized = serializer.Serialize(entries);
            Assert.Equal(Bytes.FromString(serialized), 
                         Bytes.FromString(cloudTestData["kExpectedResult"], StringEncoding.BASE64));
        }

        //[Fact]
        //public void Task KTC_18()
        //{ }

        private CloudEntry ParseEntry(Dictionary<string, dynamic> dict, int number)
        {
            string data = dict[$"kData{number}"];
            var meta = dict[$"kMeta{number}"];
            return new CloudEntry()
            {
                Name = dict[$"kName{number}"],
                Data = Bytes.FromString(data, StringEncoding.BASE64),
                Meta = meta,
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
