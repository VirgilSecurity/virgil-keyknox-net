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
        private Faker faker;

        public CloudKeyStorageTests()
        {
            this.faker = new Faker();
        }

        [Fact]
        public async Task KTC_19_RetrieveEmpty()
        {
            var identity = this.faker.Random.Guid().ToString();
            var storage = new CloudKeyStorage(IntegrationHelper.GetKeyknoxManager(identity));
            await storage.UploadAllAsync();
            var entries = storage.RetrieveAll();
            Assert.Empty(entries);
        }

        [Fact]
        public async Task KTC_20_Store()
        {
            var newManager = IntegrationHelper.GetKeyknoxManager(this.faker.Random.Guid().ToString());

            var storage = new CloudKeyStorage(newManager);
            await storage.UploadAllAsync();
            var data = this.faker.Random.Bytes(10);

            var name = this.faker.Name.FullName();
            var meta = new Dictionary<string, string>()
            {
                { "key1", "value1" },
                { "key2", "value2" } 
            };
            
            var entry = await storage.StoreAsync(name, data, meta);

            var entries = storage.RetrieveAll();

            Assert.Single(entries);
            CompareEntries(entry, entries[0]);

            var retrievedEntry = storage.Retrieve(name);
            CompareEntries(entry, retrievedEntry);
        }

        [Fact]
        public async Task KTC_21_Store()
        {
            var newManager = IntegrationHelper.GetKeyknoxManager(this.faker.Random.Guid().ToString());

            var storage = new CloudKeyStorage(newManager);
            await storage.UploadAllAsync();
            var data = this.faker.Random.Bytes(10);

            var name = this.faker.Name.FullName();
            var meta = new Dictionary<string, string>()
            {
                { "key1", "value1" },
                { "key2", "value2" } 
            };
            
            var entry = await storage.StoreAsync(name, data, meta);

            Assert.True(storage.Exists(name));

            var nameOfMissingEntry = this.faker.Name.FullName();
            Assert.False(storage.Exists(nameOfMissingEntry));

            var entries = storage.RetrieveAll();
            Assert.NotNull(entries.FirstOrDefault(el => el.Name == entry.Name));
            Assert.Null(entries.FirstOrDefault(el => el.Name == nameOfMissingEntry));
        }

        [Fact]
        public async Task KTC_22_Store()
        {
            var name = this.faker.Name.FullName();
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

            var newManager = IntegrationHelper.GetKeyknoxManager(this.faker.Random.Guid().ToString());

            var storage = new CloudKeyStorage(newManager);
            await storage.UploadAllAsync();
            await storage.StoreAsync(new List<KeyEntry> { keyEntries[0] });
            await storage.StoreAsync(keyEntries.GetRange(1, 98));

            foreach (KeyEntry entry in keyEntries.GetRange(0, 98))
            {
                var cloudEntry = storage.Retrieve(entry.Name);
                Assert.Equal(entry.Value, cloudEntry.Data);
                Assert.Equal(entry.Meta, cloudEntry.Meta);
            }

            await storage.UploadAllAsync();
            Assert.Equal(99, storage.RetrieveAll().Count);

            await storage.StoreAsync(new List<KeyEntry>() { keyEntries.Last() });
            Assert.Equal(100, storage.RetrieveAll().Count);
            foreach (KeyEntry entry in keyEntries)
            {
                var cloudEntry = storage.Retrieve(entry.Name);
                Assert.Equal(entry.Value, cloudEntry.Data);
                Assert.Equal(entry.Meta, cloudEntry.Meta);
            }

            var retrievedEntries = await storage.UploadAllAsync();
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
            var name = this.faker.Name.FullName();
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

            var newManager = IntegrationHelper.GetKeyknoxManager(this.faker.Random.Guid().ToString());
            var storage = new CloudKeyStorage(newManager);
            var cloudEntries = await storage.UploadAllAsync();
            Assert.True(cloudEntries.Count == 0);

            await storage.StoreAsync(keyEntries);
           
            var retrievedEntries = await storage.UploadAllAsync();
            Assert.Equal(100, retrievedEntries.Count);

            await storage.DeteleAllAsync();
            Assert.Empty(storage.RetrieveAll());

            retrievedEntries = await storage.UploadAllAsync();
            Assert.Empty(retrievedEntries);
        }

        [Fact]
        public async Task KTC_24_Store()
        {
            var newManager = IntegrationHelper.GetKeyknoxManager(this.faker.Random.Guid().ToString());
            var storage = new CloudKeyStorage(newManager);
            Assert.Empty(await storage.UploadAllAsync());

            await storage.DeteleAllAsync();
            Assert.Empty(storage.RetrieveAll());
            Assert.Empty(await storage.UploadAllAsync());
        }

        [Fact]
        public async Task KTC_25_Store()
        {
            var name = this.faker.Name.FullName();
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

            var newManager = IntegrationHelper.GetKeyknoxManager(this.faker.Random.Guid().ToString());

            var storage = new CloudKeyStorage(newManager);
            await storage.UploadAllAsync();
            await storage.StoreAsync(keyEntries);

            foreach (KeyEntry entry in keyEntries)
            {
                var cloudEntry = storage.Retrieve(entry.Name);
                Assert.Equal(entry.Value, cloudEntry.Data);
                Assert.Equal(entry.Meta, cloudEntry.Meta);
            }

            await storage.UploadAllAsync();
            Assert.Equal(10, storage.RetrieveAll().Count);

            await storage.DeteleAsync(keyEntries.First().Name);

            Assert.Equal(9, storage.RetrieveAll().Count);
            foreach (KeyEntry entry in keyEntries.GetRange(1, 9))
            {
                var cloudEntry = storage.Retrieve(entry.Name);
                Assert.Equal(entry.Value, cloudEntry.Data);
                Assert.Equal(entry.Meta, cloudEntry.Meta);
            }

            await storage.DeteleAsync(keyEntries[1].Name);
            await storage.DeteleAsync(keyEntries[2].Name);
            Assert.Equal(7, storage.RetrieveAll().Count);

            foreach (KeyEntry entry in keyEntries.GetRange(3, 7))
            {
                var cloudEntry = storage.Retrieve(entry.Name);
                Assert.Equal(entry.Value, cloudEntry.Data);
                Assert.Equal(entry.Meta, cloudEntry.Meta);
            }
        }

        [Fact]
        public async Task KTC_26_Store()
        {
            var name = this.faker.Name.FullName();
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

            var newManager = IntegrationHelper.GetKeyknoxManager(this.faker.Random.Guid().ToString());

            var storage = new CloudKeyStorage(newManager);
            await storage.UploadAllAsync();
            await storage.StoreAsync(keyEntries);

            foreach (KeyEntry entry in keyEntries)
            {
                var cloudEntry = storage.Retrieve(entry.Name);
                Assert.Equal(entry.Value, cloudEntry.Data);
                Assert.Equal(entry.Meta, cloudEntry.Meta);
            }

            var cloudEntries = await storage.UploadAllAsync();
            Assert.Equal(10, storage.RetrieveAll().Count);

            var newEntryOneVal = Bytes.FromString($"{keyEntries.First().Name}");
            var updatedEntry = await storage.UpdateAsync(
                keyEntries.First().Name,
                newEntryOneVal,
                null);
            Assert.Equal(updatedEntry.Meta, cloudEntries[updatedEntry.Name].Meta);
            Assert.Equal(updatedEntry.Data, cloudEntries[updatedEntry.Name].Data);

            Assert.Equal(newEntryOneVal, updatedEntry.Data);
            Assert.Null(updatedEntry.Meta);
            Assert.Equal(updatedEntry.ModificationDate.Date, DateTime.Today);

            var retrievedEntry = storage.Retrieve(keyEntries.First().Name);
            CompareEntries(retrievedEntry, updatedEntry);

            cloudEntries = await storage.UploadAllAsync();

            Assert.Equal(updatedEntry.Meta, cloudEntries[updatedEntry.Name].Meta);
            Assert.Equal(updatedEntry.Data, cloudEntries[updatedEntry.Name].Data);
        }

        [Fact]
        public async Task KTC_27_Store()
        {
            var name = this.faker.Name.FullName();
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

            var newManager = IntegrationHelper.GetKeyknoxManager(this.faker.Random.Guid().ToString());

            var storage = new CloudKeyStorage(newManager);
            await storage.UploadAllAsync();
            await storage.StoreAsync(keyEntries);

            var cloudEntries = await storage.UploadAllAsync();
            Assert.Equal(10, storage.RetrieveAll().Count);
            var crypto = new VirgilCrypto();
            var keyPair1 = crypto.GenerateKeys();
            var keyPair2 = crypto.GenerateKeys();
            await storage.UpdateRecipientsAsync(new IPublicKey[] { keyPair1.PublicKey, keyPair2.PublicKey }, keyPair1.PrivateKey);

            Assert.Equal(10, storage.RetrieveAll().Count);
            cloudEntries = await storage.UploadAllAsync();

            foreach (KeyEntry entry in keyEntries)
            {
                Assert.Equal(entry.Value, cloudEntries[entry.Name].Data);
                Assert.Equal(entry.Meta, cloudEntries[entry.Name].Meta);
            }
        }

        [Fact]
        public async Task KTC_28_SyncExceptionIfUnsynchronized()
        {
            var name = this.faker.Name.FullName();
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

            var newManager = IntegrationHelper.GetKeyknoxManager(this.faker.Random.Guid().ToString());

            var storage = new CloudKeyStorage(newManager);
          
            var ex = Record.Exception(() =>
            {
                storage.RetrieveAll();
            });
            Assert.IsType<SyncException>(ex);

             ex = Record.Exception(() =>
            {
                storage.Retrieve("some name");
            });
            Assert.IsType<SyncException>(ex);

            ex = Record.Exception(() =>
            {
                storage.Exists("some name");
            });
            Assert.IsType<SyncException>(ex);

            var exceptionAsync = Record.ExceptionAsync(async () =>
            {
                await storage.StoreAsync(new List<KeyEntry>() { keyEntries.First() });
            });
            Assert.IsType<SyncException>(await exceptionAsync);

            exceptionAsync = Record.ExceptionAsync(async () =>
            {
                await storage.StoreAsync(keyEntries);
            });
            Assert.IsType<SyncException>(await exceptionAsync);

            exceptionAsync = Record.ExceptionAsync(async () =>
            {
                await storage.UpdateAsync(keyEntries.First().Name, this.faker.Random.Bytes(2), null);
            });
            Assert.IsType<SyncException>(await exceptionAsync);

            exceptionAsync = Record.ExceptionAsync(async () =>
            {
                await storage.DeteleAsync(keyEntries.First().Name);
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
            var newManager = IntegrationHelper.GetKeyknoxManager(this.faker.Random.Guid().ToString());

            var data = this.faker.Random.Bytes(10);
            var pushedByManager = await newManager.PushValueAsync(data);
            Assert.NotNull(pushedByManager);

            var storage = new CloudKeyStorage(newManager);

            await storage.DeteleAllAsync();
            Assert.Empty(storage.RetrieveAll());
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
