using System;
using System.Collections.Generic;
using System.Linq;
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
            var newManager = IntegrationHelper.GetKeyknoxManager(faker.Random.Guid().ToString());

            var storage = new CloudKeyStorage(newManager);
            await storage.RetrieveCloudEntriesAsync();
            var data = this.faker.Random.Bytes(10);

            var name = faker.Name.FullName();
            var meta = new Dictionary<string, string>() {
                { "key1", "value1" },
                { "key2", "value2" }};
            var entry = await storage.Store(name, data, meta);

            var entries = storage.RetrieveAllEntries();

            Assert.Equal(1, entries.Count);
            CompareEntries(entry, entries[0]);

            var retrievedEntry = storage.RetrieveEntry(name);
            CompareEntries(entry, retrievedEntry);
        }

        [Fact]
        public async Task KTC_21_Store()
        {
            var newManager = IntegrationHelper.GetKeyknoxManager(faker.Random.Guid().ToString());

            var storage = new CloudKeyStorage(newManager);
            await storage.RetrieveCloudEntriesAsync();
            var data = this.faker.Random.Bytes(10);

            var name = faker.Name.FullName();
            var meta = new Dictionary<string, string>() {
                { "key1", "value1" },
                { "key2", "value2" }};
            var entry = await storage.Store(name, data, meta);

            Assert.True(storage.ExistsEntry(name));

            var nameOfMissingEntry = faker.Name.FullName();
            Assert.False(storage.ExistsEntry(nameOfMissingEntry));

            var entries = storage.RetrieveAllEntries();
            Assert.NotNull(entries.FirstOrDefault(el => el.Name == entry.Name));
            Assert.Null(entries.FirstOrDefault(el => el.Name == nameOfMissingEntry));
        }

        [Fact]
        public async Task KTC_22_Store()
        {
            var newManager = IntegrationHelper.GetKeyknoxManager(faker.Random.Guid().ToString());

            var storage = new CloudKeyStorage(newManager);
            await storage.RetrieveCloudEntriesAsync();
            var data = this.faker.Random.Bytes(10);

            var name = faker.Name.FullName();
            var meta = new Dictionary<string, string>() {
                { "key1", "value1" },
                { "key2", "value2" }};
            var entry = await storage.Store(name, data, meta);

            Assert.True(storage.ExistsEntry(name));

            var nameOfMissingEntry = faker.Name.FullName();
            Assert.False(storage.ExistsEntry(nameOfMissingEntry));

            var entries = storage.RetrieveAllEntries();
            Assert.True(entries.Contains(entry));
            Assert.Null(entries.FirstOrDefault(el => el.Name == nameOfMissingEntry));
        }

        private static void CompareEntries(CloudEntry expectedEntry, CloudEntry actualtEntry)
        {
            Assert.Equal(
                UnixTimestampConverterInMilliseconds.UnixTimestampInMillisecondsFromDateTime(expectedEntry.CreationDate), 
                UnixTimestampConverterInMilliseconds.UnixTimestampInMillisecondsFromDateTime(actualtEntry.CreationDate));
            Assert.Equal(
                UnixTimestampConverterInMilliseconds.UnixTimestampInMillisecondsFromDateTime(expectedEntry.ModificationDate),
                UnixTimestampConverterInMilliseconds.UnixTimestampInMillisecondsFromDateTime(actualtEntry.ModificationDate));
            Assert.Equal(expectedEntry.Name, actualtEntry.Name);
            Assert.Equal(expectedEntry.Data, actualtEntry.Data);
            Assert.Equal(expectedEntry.Meta, actualtEntry.Meta);
        }
    }
}
