using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bogus;
using Xunit;

namespace Keyknox.Tests
{
    public class CloudKeyStorageTests
    {
        Faker faker; 
        public CloudKeyStorageTests()
        {
            this.faker = new Faker();
        }

        [Fact]
        public async Task KTC_19_RetrieveEmpty()
        {
            var identity = faker.Random.Guid().ToString();
            var storage = new CloudKeyStorage(IntegrationHelper.GetKeyknoxManager(identity));
            await storage.RetrieveCloudEntriesAsync();
            var entries = storage.RetrieveAllEntries();
            Assert.Empty(entries);
        }

        [Fact]
        public async Task KTC_20_Store()
        {
            var identity = faker.Random.Guid().ToString();
            var storage = new CloudKeyStorage(IntegrationHelper.GetKeyknoxManager(identity));
            await storage.RetrieveCloudEntriesAsync();
            var data = this.faker.Random.Bytes(10);

            var name = faker.Random.String(5);
            var meta = new Dictionary<string, string>() {
                { "key1", "value1" },
                { "key2", "value2" }};
            var entry = await storage.Store(name, data, meta);

            var entries = storage.RetrieveAllEntries();
            Assert.Equal(1, entries.Count);
            Assert.Equal(entry, entries[0]);

            var retrievedEntry = storage.RetrieveEntry(name);
            Assert.Equal(entry, retrievedEntry);
        }
    }
}
