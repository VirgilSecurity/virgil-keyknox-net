
namespace Keyknox.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Bogus;
    using Keyknox.CloudKeyStorageException;
    using Keyknox.Utils;
    using Virgil.Crypto;
    using Virgil.CryptoAPI;
    using Virgil.SDK;
    using Virgil.SDK.Common;
    using Virgil.SDK.Web.Authorization;
    using Xunit;


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
            var entry = await storage.StoreAsync(name, data, meta);

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
            var entry = await storage.StoreAsync(name, data, meta);

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
            var name = faker.Name.FullName();
            var keyEntries = new List<KeyEntry>();
            for (int i = 0; i < 100; i++)
            {
                var entry1 = new KeyEntry(){
                    Name = $"{name}-{i}",
                    Value = BitConverter.GetBytes(i),
                    Meta = new Dictionary<string, string>() { { "key1", "value1" } }
                };
                keyEntries.Add(entry1);
            }

            var newManager = IntegrationHelper.GetKeyknoxManager(faker.Random.Guid().ToString());

            var storage = new CloudKeyStorage(newManager);
            await storage.RetrieveCloudEntriesAsync();
            await storage.StoreEntriesAsync(new List<KeyEntry> { keyEntries[0] });
            await storage.StoreEntriesAsync(keyEntries.GetRange(1, 98));

            foreach (KeyEntry entry in keyEntries.GetRange(0, 98)){
                var cloudEntry = storage.RetrieveEntry(entry.Name);
                Assert.Equal(entry.Value, cloudEntry.Data);
                Assert.Equal(entry.Meta, cloudEntry.Meta);
            }

            await storage.RetrieveCloudEntriesAsync();
            Assert.Equal(99, storage.RetrieveAllEntries().Count);

            await storage.StoreEntriesAsync(new List<KeyEntry>(){keyEntries.Last()});
            Assert.Equal(100, storage.RetrieveAllEntries().Count);
            foreach (KeyEntry entry in keyEntries)
            {
                var cloudEntry = storage.RetrieveEntry(entry.Name);
                Assert.Equal(entry.Value, cloudEntry.Data);
                Assert.Equal(entry.Meta, cloudEntry.Meta);
            }

            var retrievedEntries = await storage.RetrieveCloudEntriesAsync();
            Assert.Equal(100, retrievedEntries.Count);
            foreach (KeyEntry entry in keyEntries)
            {
                var cloudEntry = retrievedEntries[entry.Name];
                Assert.Equal(entry.Value, cloudEntry.Data);
                Assert.Equal(entry.Meta, cloudEntry.Meta);
            }
        }


        [Fact]
        public async Task KTC_23_Store()
        {
            var name = faker.Name.FullName();
            var keyEntries = new List<KeyEntry>();
            for (int i = 0; i < 100; i++)
            {
                var entry1 = new KeyEntry()
                {
                    Name = $"{name}-{i}",
                    Value = BitConverter.GetBytes(i),
                    Meta = new Dictionary<string, string>() { { "key1", "value1" } }
                };
                keyEntries.Add(entry1);
            }

            var newManager = IntegrationHelper.GetKeyknoxManager(faker.Random.Guid().ToString());
            var storage = new CloudKeyStorage(newManager);
            var cloudEntries = await storage.RetrieveCloudEntriesAsync();
            Assert.True(cloudEntries.Count == 0);


            await storage.StoreEntriesAsync(keyEntries);
           
            var retrievedEntries = await storage.RetrieveCloudEntriesAsync();
            Assert.Equal(100, retrievedEntries.Count);

            await storage.DeteleAllAsync();
            Assert.Empty(storage.RetrieveAllEntries());

            retrievedEntries = await storage.RetrieveCloudEntriesAsync();
            Assert.Empty(retrievedEntries);
        }


        [Fact]
        public async Task KTC_24_Store()
        {
            var newManager = IntegrationHelper.GetKeyknoxManager(faker.Random.Guid().ToString());
            var storage = new CloudKeyStorage(newManager);
            Assert.Empty(await storage.RetrieveCloudEntriesAsync());

            await storage.DeteleAllAsync();
            Assert.Empty(storage.RetrieveAllEntries());
            Assert.Empty(await storage.RetrieveCloudEntriesAsync());
        }

        [Fact]
        public async Task KTC_25_Store()
        {
            var name = faker.Name.FullName();
            var keyEntries = new List<KeyEntry>();
            for (int i = 0; i < 10; i++)
            {
                var entry1 = new KeyEntry()
                {
                    Name = $"{name}-{i}",
                    Value = BitConverter.GetBytes(i),
                    Meta = new Dictionary<string, string>() { { "key1", "value1" } }
                };
                keyEntries.Add(entry1);
            }

            var newManager = IntegrationHelper.GetKeyknoxManager(faker.Random.Guid().ToString());

            var storage = new CloudKeyStorage(newManager);
            await storage.RetrieveCloudEntriesAsync();
            await storage.StoreEntriesAsync( keyEntries );

            foreach (KeyEntry entry in keyEntries)
            {
                var cloudEntry = storage.RetrieveEntry(entry.Name);
                Assert.Equal(entry.Value, cloudEntry.Data);
                Assert.Equal(entry.Meta, cloudEntry.Meta);
            }

            await storage.RetrieveCloudEntriesAsync();
            Assert.Equal(10, storage.RetrieveAllEntries().Count);

            await storage.DeteleEntryAsync(keyEntries.First().Name);

            Assert.Equal(9, storage.RetrieveAllEntries().Count);
            foreach (KeyEntry entry in keyEntries.GetRange(1, 9))
            {
                var cloudEntry = storage.RetrieveEntry(entry.Name);
                Assert.Equal(entry.Value, cloudEntry.Data);
                Assert.Equal(entry.Meta, cloudEntry.Meta);
            }

            await storage.DeteleEntryAsync(keyEntries[1].Name);
            await storage.DeteleEntryAsync(keyEntries[2].Name);
            Assert.Equal(7, storage.RetrieveAllEntries().Count);

            foreach (KeyEntry entry in keyEntries.GetRange(3, 7))
            {
                var cloudEntry = storage.RetrieveEntry(entry.Name);
                Assert.Equal(entry.Value, cloudEntry.Data);
                Assert.Equal(entry.Meta, cloudEntry.Meta);
            }
        }

        [Fact]
        public async Task KTC_26_Store()
        {
            var name = faker.Name.FullName();
            var keyEntries = new List<KeyEntry>();
            for (int i = 0; i < 10; i++)
            {
                var entry1 = new KeyEntry()
                {
                    Name = $"{name}-{i}",
                    Value = BitConverter.GetBytes(i),
                    Meta = new Dictionary<string, string>() { { "key1", "value1" } }
                };
                keyEntries.Add(entry1);
            }

            var newManager = IntegrationHelper.GetKeyknoxManager(faker.Random.Guid().ToString());

            var storage = new CloudKeyStorage(newManager);
            await storage.RetrieveCloudEntriesAsync();
            await storage.StoreEntriesAsync(keyEntries);

            foreach (KeyEntry entry in keyEntries)
            {
                var cloudEntry = storage.RetrieveEntry(entry.Name);
                Assert.Equal(entry.Value, cloudEntry.Data);
                Assert.Equal(entry.Meta, cloudEntry.Meta);
            }

            var cloudEntries = await storage.RetrieveCloudEntriesAsync();
            Assert.Equal(10, storage.RetrieveAllEntries().Count);

            var newEntryOneVal = Bytes.FromString($"{keyEntries.First().Name}");
            var updatedEntry = await storage.UpdateEntryAsync(
                keyEntries.First().Name,
                newEntryOneVal,
                null);
            Assert.Equal(updatedEntry.Meta, cloudEntries[updatedEntry.Name].Meta);
            Assert.Equal(updatedEntry.Data, cloudEntries[updatedEntry.Name].Data);

            Assert.Equal(newEntryOneVal, updatedEntry.Data);
            Assert.Null(updatedEntry.Meta);
            Assert.Equal(updatedEntry.ModificationDate.Date, DateTime.Today);

            var retrievedEntry = storage.RetrieveEntry(keyEntries.First().Name);
            CompareEntries(retrievedEntry, updatedEntry);

            cloudEntries = await storage.RetrieveCloudEntriesAsync();

            Assert.Equal(updatedEntry.Meta, cloudEntries[updatedEntry.Name].Meta);
            Assert.Equal(updatedEntry.Data, cloudEntries[updatedEntry.Name].Data);
        }

        [Fact]
        public async Task KTC_27_Store()
        {
            var name = faker.Name.FullName();
            var keyEntries = new List<KeyEntry>();
            for (int i = 0; i < 10; i++)
            {
                var entry1 = new KeyEntry()
                {
                    Name = $"{name}-{i}",
                    Value = BitConverter.GetBytes(i),
                    Meta = new Dictionary<string, string>() { { "key1", "value1" } }
                };
                keyEntries.Add(entry1);
            }

            var newManager = IntegrationHelper.GetKeyknoxManager(faker.Random.Guid().ToString());

            var storage = new CloudKeyStorage(newManager);
            await storage.RetrieveCloudEntriesAsync();
            await storage.StoreEntriesAsync(keyEntries);

            var cloudEntries = await storage.RetrieveCloudEntriesAsync();
            Assert.Equal(10, storage.RetrieveAllEntries().Count);
            var crypto = new VirgilCrypto();
            var keyPair1 = crypto.GenerateKeys();
            var keyPair2 = crypto.GenerateKeys();
            await storage.UpdateRecipientsAsync(new IPublicKey[] { keyPair1.PublicKey, keyPair2.PublicKey }, keyPair1.PrivateKey);

            Assert.Equal(10, storage.RetrieveAllEntries().Count);
            cloudEntries = await storage.RetrieveCloudEntriesAsync();

            foreach (KeyEntry entry in keyEntries)
            {
                Assert.Equal(entry.Value, cloudEntries[entry.Name].Data);
                Assert.Equal(entry.Meta, cloudEntries[entry.Name].Meta);
            }
        }


        [Fact]
        public async Task KTC_28_SyncExceptionIfUnsynchronized()
        {
            var name = faker.Name.FullName();
            var keyEntries = new List<KeyEntry>();
            for (int i = 0; i < 10; i++)
            {
                var entry1 = new KeyEntry()
                {
                    Name = $"{name}-{i}",
                    Value = BitConverter.GetBytes(i),
                    Meta = new Dictionary<string, string>() { { "key1", "value1" } }
                };
                keyEntries.Add(entry1);
            }

            var newManager = IntegrationHelper.GetKeyknoxManager(faker.Random.Guid().ToString());

            var storage = new CloudKeyStorage(newManager);
          
            var ex = Record.Exception(() =>
            {
                storage.RetrieveAllEntries();
            });
            Assert.IsType<SyncException>(ex);

             ex = Record.Exception(() =>
            {
                storage.RetrieveEntry("some name");
            });
            Assert.IsType<SyncException>(ex);

            ex = Record.Exception(() =>
            {
                storage.ExistsEntry("some name");
            });
            Assert.IsType<SyncException>(ex);


            var exceptionAsync = Record.ExceptionAsync( async () =>
            {
                await storage.StoreEntriesAsync(new List<KeyEntry>() { keyEntries.First() });
            });
            Assert.IsType<SyncException>(await exceptionAsync);


            exceptionAsync = Record.ExceptionAsync(async () =>
            {
                await storage.StoreEntriesAsync(keyEntries);
            });
            Assert.IsType<SyncException>(await exceptionAsync);


            exceptionAsync = Record.ExceptionAsync(async () =>
            {
                await storage.UpdateEntryAsync(keyEntries.First().Name, faker.Random.Bytes(2), null);
            });
            Assert.IsType<SyncException>(await exceptionAsync);


            exceptionAsync = Record.ExceptionAsync(async () =>
            {
                await storage.DeteleEntryAsync(keyEntries.First().Name);
            });
            Assert.IsType<SyncException>(await exceptionAsync);

            var crypto = new VirgilCrypto();
            var keyPair1 = crypto.GenerateKeys();
            var keyPair2 = crypto.GenerateKeys();

            exceptionAsync = Record.ExceptionAsync(async () =>
            {
                await storage.UpdateRecipientsAsync(
                    new IPublicKey[] { keyPair1.PublicKey, keyPair2.PublicKey },
                    keyPair1.PrivateKey);
            });
            Assert.IsType<SyncException>(await exceptionAsync);
        }


        [Fact]
        public async Task KTC_41_DeeleteInvalidData()
        { 
            var newManager = IntegrationHelper.GetKeyknoxManager(faker.Random.Guid().ToString());

            var data = this.faker.Random.Bytes(10);
            var pushedByManager = await newManager.PushValueAsync(data);
            Assert.NotNull(pushedByManager);

            var storage = new CloudKeyStorage(newManager);

            await storage.DeteleAllAsync();
            Assert.Empty(storage.RetrieveAllEntries());
        }

        private static void CompareEntries(CloudEntry expectedEntry, CloudEntry actualtEntry)
        {
            Assert.Equal(
                UnixTimestampConverterInMilliseconds.UnixTimestampFrom(expectedEntry.CreationDate),
                UnixTimestampConverterInMilliseconds.UnixTimestampFrom(actualtEntry.CreationDate));
            Assert.Equal(
                UnixTimestampConverterInMilliseconds.UnixTimestampFrom(expectedEntry.ModificationDate),
                UnixTimestampConverterInMilliseconds.UnixTimestampFrom(actualtEntry.ModificationDate));
            Assert.Equal(expectedEntry.Name, actualtEntry.Name);
            Assert.Equal(expectedEntry.Data, actualtEntry.Data);
            Assert.Equal(expectedEntry.Meta, actualtEntry.Meta);
        }
    }
}
