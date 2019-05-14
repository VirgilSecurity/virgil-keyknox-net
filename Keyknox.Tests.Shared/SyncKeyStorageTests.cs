namespace Keyknox.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Bogus;
    using Keyknox.CloudKeyStorageException;
    using Virgil.Crypto;
    using Virgil.CryptoAPI;
    using Virgil.SDK;
    using Virgil.SDK.Web.Authorization;
    using Xunit;

    public class SyncKeyStorageTests
    {
        private Faker faker = new Faker();

        public SyncKeyStorageTests()
        {
            SecureStorage.StorageIdentity = "keyknox.tests";
        }

        [Fact]
        public async Task KTC_29()
        { 
            var identity = this.faker.Random.Guid().ToString();
            var newManager = IntegrationHelper.GetKeyknoxManager(this.faker.Random.Guid().ToString());
            var cloudStorage = new CloudKeyStorage(newManager);
            var localStorage = IntegrationHelper.GetLocalKeyStorage(identity);

            var syncStorage = new SyncKeyStorage(identity, cloudStorage, localStorage);
            await syncStorage.SynchronizeAsync();

            Assert.Empty(syncStorage.RetrieveAll());

            var name = this.faker.Name.FullName();
            var meta = new Dictionary<string, string>()
            {
                { "key1", "value1" },
                { "key2", "value2" }
            };

            var data = this.faker.Random.Bytes(10);
            var cloudEntry = await cloudStorage.StoreAsync(name, data, meta);

            await syncStorage.SynchronizeAsync();

            var keyEntries = syncStorage.RetrieveAll();
            Assert.Single(keyEntries);
            this.AreEqual(keyEntries.First(), cloudEntry);
            var name2 = this.faker.Name.FullName();
            var meta2 = new Dictionary<string, string>()
            {
                { "key1", "value1" },
                { "key2", "value2" }
            };

            var data2 = this.faker.Random.Bytes(10);

            var cloudEntry2 = await cloudStorage.StoreAsync(name2, data2, meta2);
            await syncStorage.SynchronizeAsync();
            keyEntries = syncStorage.RetrieveAll();
            Assert.Equal(2, keyEntries.Count);
            this.AreEqual(keyEntries.Find((obj) => obj.Name == cloudEntry.Name), cloudEntry);
            this.AreEqual(keyEntries.Find((obj) => obj.Name == cloudEntry2.Name), cloudEntry2);

            localStorage.Delete(cloudEntry.Name);
            localStorage.Delete(cloudEntry2.Name);

            await syncStorage.SynchronizeAsync();

            Assert.Equal(2, localStorage.Names().Length);
            var localEntry = localStorage.Load(cloudEntry.Name);
            var localEntry2 = localStorage.Load(cloudEntry2.Name);

            this.AreEqual(localEntry, cloudEntry);
            this.AreEqual(localEntry2, cloudEntry2);
            await cloudStorage.DeteleAsync(cloudEntry.Name);

            await syncStorage.SynchronizeAsync();
            Assert.Single(syncStorage.RetrieveAll());
            this.AreEqual(syncStorage.RetrieveAll().First(), cloudEntry2);

            var updatedData = this.faker.Random.Bytes(1);
            await cloudStorage.UpdateAsync(cloudEntry2.Name, updatedData);

            await syncStorage.SynchronizeAsync();

            Assert.Single(localStorage.Names());
            localEntry = localStorage.Load(cloudEntry2.Name);

            Assert.Equal(updatedData, localEntry.Value);

            await cloudStorage.DeteleAllAsync();

            await syncStorage.SynchronizeAsync();

            Assert.Empty(localStorage.Names());
        }

        [Fact]
        public async Task KTC_30()
        {
            var identity = this.faker.Random.Guid().ToString();
            var newManager = IntegrationHelper.GetKeyknoxManager(this.faker.Random.Guid().ToString());
            var cloudStorage = new CloudKeyStorage(newManager);
            var localStorage = IntegrationHelper.GetLocalKeyStorage(identity);

            var syncStorage = new SyncKeyStorage(identity, cloudStorage, localStorage);
            await syncStorage.SynchronizeAsync();

            var name = this.faker.Name.FullName();
            var meta = new Dictionary<string, string>() { { "key1", "value1" } };

            var data = this.faker.Random.Bytes(2);

            await syncStorage.StoreAsync(name, data, meta);

            var cloudEntries = cloudStorage.RetrieveAll();
            Assert.Single(cloudEntries);
            Assert.Equal(data, cloudEntries.First().Data);
            Assert.Equal(meta, cloudEntries.First().Meta);
            Assert.Equal(name, cloudEntries.First().Name);

            Assert.Single(localStorage.Names());
            var localEntry = localStorage.Load(name);
            Assert.Equal(data, localEntry.Value);
        }

        [Fact]
        public async Task KTC_31()
        {
            var identity = this.faker.Random.Guid().ToString();
            var newManager = IntegrationHelper.GetKeyknoxManager(this.faker.Random.Guid().ToString());
            var cloudStorage = new CloudKeyStorage(newManager);
            var localStorage = IntegrationHelper.GetLocalKeyStorage(identity);

            var syncStorage = new SyncKeyStorage(identity, cloudStorage, localStorage);
            await syncStorage.SynchronizeAsync();

            var keyEntries = new List<KeyEntry>();
            keyEntries.Add(new KeyEntry()
            {
                Name = this.faker.Name.FullName(),
                Meta = new Dictionary<string, string>() { { "key1", "value1" } },
                Value = this.faker.Random.Bytes(2)
            });
            keyEntries.Add(new KeyEntry()
            {
                Name = this.faker.Name.FullName(),
                Meta = new Dictionary<string, string>() { { "key2", "value2" } },
                Value = this.faker.Random.Bytes(2)
            });
            await syncStorage.StoreAsync(keyEntries);

            var cloudEntries = cloudStorage.RetrieveAll();
            Assert.Equal(2, cloudEntries.Count);
            this.AreEqual(keyEntries.First(), cloudEntries.Find((obj) => obj.Name == keyEntries.First().Name));
            this.AreEqual(keyEntries.Last(), cloudEntries.Find((obj) => obj.Name == keyEntries.Last().Name));

            Assert.Equal(2, localStorage.Names().Length);
            this.AreEqual(keyEntries.First(), localStorage.Load(keyEntries.First().Name));

            this.AreEqual(keyEntries.Last(), localStorage.Load(keyEntries.Last().Name));

            await syncStorage.DeleteAsync(keyEntries.First().Name);

            cloudEntries = cloudStorage.RetrieveAll();
            Assert.Single(cloudEntries);
            this.AreEqual(keyEntries.Last(), cloudEntries.Last());
           
            Assert.Single(localStorage.Names());
            this.AreEqual(keyEntries.Last(), localStorage.Load(keyEntries.Last().Name));
        }

        [Fact]
        public async Task KTC_32()
        {
            var identity = this.faker.Random.Guid().ToString();
            var newManager = IntegrationHelper.GetKeyknoxManager(this.faker.Random.Guid().ToString());
            var cloudStorage = new CloudKeyStorage(newManager);
            var localStorage = IntegrationHelper.GetLocalKeyStorage(identity);

            var syncStorage = new SyncKeyStorage(identity, cloudStorage, localStorage);
            await syncStorage.SynchronizeAsync();

            var name = this.faker.Name.FullName();
            var meta = new Dictionary<string, string>() { { "key1", "value1" } };
            var value = this.faker.Random.Bytes(2);
          
            await syncStorage.StoreAsync(name, value, meta);

            var cloudEntries = cloudStorage.RetrieveAll();
            Assert.Single(cloudEntries);
            Assert.Equal(value, cloudEntries.First().Data);
            Assert.Equal(meta, cloudEntries.First().Meta);
            Assert.Equal(name, cloudEntries.First().Name);

            Assert.Single(localStorage.Names());
            var localEntry = localStorage.Load(name);

            Assert.Equal(value, localEntry.Value);
            Assert.Equal(name, localEntry.Name);

            await syncStorage.UpdateAsync(name, value, meta);
        }

        [Fact]
        public async Task KTC_33()
        {
            var identity = this.faker.Random.Guid().ToString();
            var newManager = IntegrationHelper.GetKeyknoxManager(this.faker.Random.Guid().ToString());
            var cloudStorage = new CloudKeyStorage(newManager);
            var localStorage = IntegrationHelper.GetLocalKeyStorage(identity);

            var syncStorage = new SyncKeyStorage(identity, cloudStorage, localStorage);
            await syncStorage.SynchronizeAsync();

            var name = this.faker.Name.FullName();
            var meta = new Dictionary<string, string>() { { "key1", "value1" } };
            var value = this.faker.Random.Bytes(2);

            await syncStorage.StoreAsync(name, value, meta);
            var crypto = new VirgilCrypto();
            var keyPair1 = crypto.GenerateKeys();
            var keyPair2 = crypto.GenerateKeys();

            var decryptedVal = await newManager.PullValueAsync();
            await syncStorage.UpdateRecipientsAsync(new IPublicKey[] { keyPair1.PublicKey, keyPair2.PublicKey }, keyPair1.PrivateKey);
            var newDecryptedVal = await newManager.PullValueAsync();

            // keys were changed so meta should be different
            Assert.Equal(decryptedVal.Value, newDecryptedVal.Value);
            Assert.NotEqual(decryptedVal.Meta, newDecryptedVal.Meta);
            Assert.NotEqual(decryptedVal.Version, newDecryptedVal.Version);

            await syncStorage.SynchronizeAsync();

            var entry = syncStorage.Retrieve(name);
            Assert.Equal(value, entry.Value);
            Assert.Equal(meta, entry.Meta);
        }

        [Fact]
        public async Task KTC_34()
        {
            var identity = this.faker.Random.Guid().ToString();
            var newManager = IntegrationHelper.GetKeyknoxManager(this.faker.Random.Guid().ToString());
            var cloudStorage = new CloudKeyStorage(newManager);
            var localStorage = IntegrationHelper.GetLocalKeyStorage(identity);

            var syncStorage = new SyncKeyStorage(identity, cloudStorage, localStorage);
            await syncStorage.SynchronizeAsync();

            var keyEntries = new List<KeyEntry>();
            keyEntries.Add(new KeyEntry()
            {
                Name = this.faker.Name.FullName(),
                Meta = new Dictionary<string, string>() { { "key1", "value1" } },
                Value = this.faker.Random.Bytes(2)
            });
            keyEntries.Add(new KeyEntry()
            {
                Name = this.faker.Name.FullName(),
                Meta = new Dictionary<string, string>() { { "key2", "value2" } },
                Value = this.faker.Random.Bytes(2)
            });
            var syncEntries = await syncStorage.StoreAsync(keyEntries);
            var cloudEntries = cloudStorage.RetrieveAll();
            Assert.Equal(2, syncEntries.Count);
            this.AreEqual(keyEntries.Find((obj) => obj.Name == syncEntries.First().Name), syncEntries.First());
            this.AreEqual(keyEntries.Find((obj) => obj.Name == syncEntries.Last().Name), syncEntries.Last());

            Assert.Equal(2, cloudEntries.Count);
            this.AreEqual(keyEntries.First(), cloudEntries.Find((obj) => obj.Name == keyEntries.First().Name));
            this.AreEqual(keyEntries.Last(), cloudEntries.Find((obj) => obj.Name == keyEntries.Last().Name));

            Assert.Equal(2, localStorage.Names().Length);
            var localEntry = localStorage.Load(keyEntries.First().Name);
            this.AreEqual(keyEntries.First(), localEntry);

            var localEntry2 = localStorage.Load(keyEntries.Last().Name);
            this.AreEqual(keyEntries.Last(), localEntry2);
        }

        [Fact]
        public async Task KTC_35()
        {
            var identity = this.faker.Random.Guid().ToString();
            var newManager = IntegrationHelper.GetKeyknoxManager(this.faker.Random.Guid().ToString());
            var cloudStorage = new CloudKeyStorage(newManager);
            var localStorage = IntegrationHelper.GetLocalKeyStorage(identity);

            var syncStorage = new SyncKeyStorage(identity, cloudStorage, localStorage);
            await syncStorage.SynchronizeAsync();

            var keyEntries = new List<KeyEntry>();
            keyEntries.Add(new KeyEntry()
            {
                Name = this.faker.Name.FullName(),
                Meta = new Dictionary<string, string>() { { "key1", "value1" } },
                Value = this.faker.Random.Bytes(2)
            });
            keyEntries.Add(new KeyEntry()
            {
                Name = this.faker.Name.FullName(),
                Meta = new Dictionary<string, string>() { { "key2", "value2" } },
                Value = this.faker.Random.Bytes(2)
            });

            keyEntries.Add(new KeyEntry()
            {
                Name = this.faker.Name.FullName(),
                Meta = new Dictionary<string, string>() { { "key3", "value3" } },
                Value = this.faker.Random.Bytes(2)
            });

            var syncEntries = await syncStorage.StoreAsync(keyEntries);

            await syncStorage.DeleteAsync(keyEntries[0].Name);
            await syncStorage.DeleteAsync(keyEntries[1].Name);
            var cloudEntries = cloudStorage.RetrieveAll();
            Assert.Single(syncStorage.RetrieveAll());

            this.AreEqual(keyEntries.First(), syncEntries.First());
            Assert.Single(cloudEntries);
            this.AreEqual(keyEntries.Last(), cloudEntries.First());

            Assert.Single(localStorage.Names());
            var localEntry = localStorage.Load(localStorage.Names().First());
            this.AreEqual(keyEntries.Last(), localEntry);
        }

        [Fact]
        public async Task KTC_36()
        {
            var identity = this.faker.Random.Guid().ToString();
            var newManager = IntegrationHelper.GetKeyknoxManager(this.faker.Random.Guid().ToString());
            var cloudStorage = new CloudKeyStorage(newManager);
            var localStorage = IntegrationHelper.GetLocalKeyStorage(identity);

            var syncStorage = new SyncKeyStorage(identity, cloudStorage, localStorage);
            await syncStorage.SynchronizeAsync();

            var keyEntries = new List<KeyEntry>();
            keyEntries.Add(new KeyEntry()
            {
                Name = this.faker.Name.FullName(),
                Meta = new Dictionary<string, string>() { { "key1", "value1" } },
                Value = this.faker.Random.Bytes(2)
            });
            keyEntries.Add(new KeyEntry()
            {
                Name = this.faker.Name.FullName(),
                Meta = new Dictionary<string, string>() { { "key2", "value2" } },
                Value = this.faker.Random.Bytes(2)
            });

            await syncStorage.StoreAsync(keyEntries);

            localStorage.Delete(keyEntries[0].Name);
            localStorage.Delete(keyEntries[1].Name);

            localStorage.Store(keyEntries[0]);
            localStorage.Store(keyEntries[1]);

            var syncEntries = syncStorage.RetrieveAll();

            Assert.Equal(2, syncEntries.Count);
            this.AreEqual(keyEntries.First(), syncEntries.Find((obj) => obj.Name == keyEntries.First().Name));
            this.AreEqual(keyEntries.Last(), syncEntries.Find((obj) => obj.Name == keyEntries.Last().Name));
        }

        [Fact]
        public async Task KTC_37()
        {
            var identity = this.faker.Random.Guid().ToString();
            var newManager = IntegrationHelper.GetKeyknoxManager(this.faker.Random.Guid().ToString());
            var cloudStorage = new CloudKeyStorage(newManager);
            var localStorage = IntegrationHelper.GetLocalKeyStorage(identity);

            var syncStorage = new SyncKeyStorage(identity, cloudStorage, localStorage);
            await syncStorage.SynchronizeAsync();

            var keyEntries = new List<KeyEntry>();
            keyEntries.Add(new KeyEntry()
            {
                Name = this.faker.Name.FullName(),
                Meta = new Dictionary<string, string>() { { "key1", "value1" } },
                Value = this.faker.Random.Bytes(2)
            });
            keyEntries.Add(new KeyEntry()
            {
                Name = this.faker.Name.FullName(),
                Meta = new Dictionary<string, string>() { { "key2", "value2" } },
                Value = this.faker.Random.Bytes(2)
            });

            await syncStorage.StoreAsync(keyEntries);

            Assert.Equal(2, syncStorage.RetrieveAll().Count);

            await syncStorage.DeleteAllAsync();

            Assert.Empty(syncStorage.RetrieveAll());
            Assert.Empty(localStorage.Names());
            Assert.Empty(cloudStorage.RetrieveAll());
        }

        [Fact]
        public async Task KTC_38()
        {
            var identity = this.faker.Random.Guid().ToString();
            var newManager = IntegrationHelper.GetKeyknoxManager(this.faker.Random.Guid().ToString());
            var cloudStorage = new CloudKeyStorage(newManager);
            var localStorage = IntegrationHelper.GetLocalKeyStorage(identity);

            var syncStorage = new SyncKeyStorage(identity, cloudStorage, localStorage);
            await syncStorage.SynchronizeAsync();

            await syncStorage.DeleteAllAsync();

            Assert.Empty(syncStorage.RetrieveAll());
            Assert.Empty(localStorage.Names());
            Assert.Empty(cloudStorage.RetrieveAll());
         }

        [Fact]
        public async Task KTC_39()
        {
            var identity = this.faker.Random.Guid().ToString();
            var newManager = IntegrationHelper.GetKeyknoxManager(this.faker.Random.Guid().ToString());
            var cloudStorage = new CloudKeyStorage(newManager);
            var localStorage = IntegrationHelper.GetLocalKeyStorage(identity);

            var syncStorage = new SyncKeyStorage(identity, cloudStorage, localStorage);
            await syncStorage.SynchronizeAsync();

            var name = this.faker.Name.FullName();
            var meta = new Dictionary<string, string>() { { "key1", "value1" } };

            var data = this.faker.Random.Bytes(2);

            await syncStorage.StoreAsync(name, data, meta);
            Assert.NotEmpty(syncStorage.RetrieveAll());
            Assert.NotEmpty(cloudStorage.RetrieveAll());
            Assert.NotEmpty(localStorage.Names());

            var exception = Record.Exception(() =>
            {
               syncStorage.Retrieve("missing entry name");
            });
            Assert.IsType<Virgil.SDK.KeyNotFoundException>(exception);
           
            exception = Record.Exception(() =>
            {
                cloudStorage.Retrieve("missing entry name");
            });
            Assert.IsType<MissingEntryException>(exception);

            exception = Record.Exception(() =>
            {
                localStorage.Load("missing entry name");
            });
            Assert.IsType<Virgil.SDK.KeyNotFoundException>(exception);

            exception = Record.Exception(() =>
            {
                localStorage.LoadFull("missing entry name");
            });
            Assert.IsType<Virgil.SDK.KeyNotFoundException>(exception);
        }

        [Fact]
        public async Task KTC_40()
        {
            var identity = this.faker.Random.Guid().ToString();
            var newManager = IntegrationHelper.GetKeyknoxManager(this.faker.Random.Guid().ToString());
            var cloudStorage = new CloudKeyStorage(newManager);
            var localStorage = IntegrationHelper.GetLocalKeyStorage(identity);

            var syncStorage = new SyncKeyStorage(identity, cloudStorage, localStorage);
            await syncStorage.SynchronizeAsync();

            var keyEntry = new KeyEntry()
            {
                Name = this.faker.Name.FullName(),
                Meta = new Dictionary<string, string>() { { "key1", "value1" } },
                Value = this.faker.Random.Bytes(2)
            };

            localStorage.Store(keyEntry);

            var exception = Record.ExceptionAsync(async () =>
            {
                await syncStorage.DeleteAsync(keyEntry.Name);
            });

            Assert.IsType<MissingEntryException>(await exception);

            exception = Record.ExceptionAsync(async () =>
            {
                await syncStorage.DeleteAsync(new List<string>() { keyEntry.Name });
            });
            Assert.IsType<MissingEntryException>(await exception);

            exception = Record.ExceptionAsync(async () =>
            {
                await syncStorage.StoreAsync(keyEntry.Name, keyEntry.Value, keyEntry.Meta);
            });
            Assert.IsType<NonUniqueEntryException>(await exception);

            exception = Record.ExceptionAsync(async () =>
            {
                await syncStorage.StoreAsync(new List<KeyEntry>() { keyEntry });
            });
            Assert.IsType<NonUniqueEntryException>(await exception);

            Assert.True(syncStorage.Exists(keyEntry.Name));

            Assert.NotNull(syncStorage.Retrieve(keyEntry.Name));

            Assert.NotNull(syncStorage.RetrieveAll());

            exception = Record.ExceptionAsync(async () =>
            {
                await syncStorage.UpdateAsync(keyEntry.Name, keyEntry.Value, keyEntry.Meta);
            });
            Assert.IsType<MissingEntryException>(await exception);
        }

        private void AreEqual(KeyEntry expected, KeyEntry actual)
        {
            Assert.Equal(expected.Value, actual.Value);
            Assert.Equal(expected.Meta, actual.Meta);
            Assert.Equal(expected.Name, actual.Name);
        }

        private void AreEqual(KeyEntry expected, CloudEntry actual)
        {
            Assert.Equal(expected.Value, actual.Data);
            Assert.Equal(expected.Meta, actual.Meta);
            Assert.Equal(expected.Name, actual.Name);
        }
    }
}
