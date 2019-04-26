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

namespace Keyknox.Tests
{
    public class SyncKeyStorageTests
    {
        Faker faker = new Faker();

        public SyncKeyStorageTests()
        {
            SecureStorage.StorageIdentity = "keyknox.tests";
        }

        [Fact]
        public async Task KTC_29()
        { 
            var identity = faker.Random.Guid().ToString();
            var newManager = IntegrationHelper.GetKeyknoxManager(faker.Random.Guid().ToString());
            var cloudStorage = new CloudKeyStorage(newManager);
            var localStorage = new LocalKeyStorage(identity);

            var syncStorage = new SyncKeyStorage(identity, cloudStorage, localStorage);
            await syncStorage.SynchronizeAsync();

            Assert.Empty(syncStorage.RetrieveAllEntries());

            var name = faker.Name.FullName();
            var meta = new Dictionary<string, string>() {
                { "key1", "value1" },
                { "key2", "value2" }};

            var data = this.faker.Random.Bytes(10);
            var cloudEntry = await cloudStorage.StoreAsync(name, data, meta);

            await syncStorage.SynchronizeAsync();

            var keyEntries = syncStorage.RetrieveAllEntries();
            Assert.Equal(1, keyEntries.Count);
            AreEqual(keyEntries.First(), cloudEntry);
            var name2 = faker.Name.FullName();
            var meta2 = new Dictionary<string, string>() {
                { "key1", "value1" },
                { "key2", "value2" }};

            var data2 = this.faker.Random.Bytes(10);

            var cloudEntry2 = await cloudStorage.StoreAsync(name2, data2, meta2);
            await syncStorage.SynchronizeAsync();
            keyEntries = syncStorage.RetrieveAllEntries();
            Assert.Equal(2, keyEntries.Count);
            AreEqual(keyEntries.Find((obj) => obj.Name == cloudEntry.Name), cloudEntry);
            AreEqual(keyEntries.Find((obj) => obj.Name == cloudEntry2.Name), cloudEntry2);

            localStorage.Delete(cloudEntry.Name);
            localStorage.Delete(cloudEntry2.Name);

            await syncStorage.SynchronizeAsync();

            Assert.Equal(2, localStorage.Names().Length);
            var localEntry = localStorage.Load(cloudEntry.Name);
            var localEntry2 = localStorage.Load(cloudEntry2.Name);


            AreEqual(localEntry, cloudEntry);
            AreEqual(localEntry2, cloudEntry2);
            await cloudStorage.DeteleEntryAsync(cloudEntry.Name);

            await syncStorage.SynchronizeAsync();
            Assert.Equal(1, syncStorage.RetrieveAllEntries().Count);
            AreEqual(syncStorage.RetrieveAllEntries().First(), cloudEntry2);

            var updatedData = this.faker.Random.Bytes(1);
            await cloudStorage.UpdateEntryAsync(cloudEntry2.Name, updatedData);

            await syncStorage.SynchronizeAsync();

            Assert.Equal(1, localStorage.Names().Length);
            localEntry = localStorage.Load(cloudEntry2.Name);

            Assert.Equal(updatedData, localEntry.Value);

            await cloudStorage.DeteleAllAsync();

            await syncStorage.SynchronizeAsync();

            Assert.Empty(localStorage.Names());
        }

        [Fact]
        public async Task KTC_30()
        {
            var identity = faker.Random.Guid().ToString();
            var newManager = IntegrationHelper.GetKeyknoxManager(faker.Random.Guid().ToString());
            var cloudStorage = new CloudKeyStorage(newManager);
            var localStorage = new LocalKeyStorage(identity);

            var syncStorage = new SyncKeyStorage(identity, cloudStorage, localStorage);
            await syncStorage.SynchronizeAsync();

            var name = faker.Name.FullName();
            var meta = new Dictionary<string, string>() {
                { "key1", "value1" }};

            var data = this.faker.Random.Bytes(2);

            await syncStorage.StoreEntryAsync(name, data, meta);

            var cloudEntries = cloudStorage.RetrieveAllEntries();

            Assert.Equal(1, cloudEntries.Count);
            Assert.Equal(data, cloudEntries.First().Data);
            Assert.Equal(meta, cloudEntries.First().Meta);
            Assert.Equal(name, cloudEntries.First().Name);


            Assert.Equal(1, localStorage.Names().Length);
            var localEntry = localStorage.Load(name);
            Assert.Equal(data, localEntry.Value);
        }

        [Fact]
        public async Task KTC_31()
        {
            var identity = faker.Random.Guid().ToString();
            var newManager = IntegrationHelper.GetKeyknoxManager(faker.Random.Guid().ToString());
            var cloudStorage = new CloudKeyStorage(newManager);
            var localStorage = new LocalKeyStorage(identity);

            var syncStorage = new SyncKeyStorage(identity, cloudStorage, localStorage);
            await syncStorage.SynchronizeAsync();

            var keyEntries = new List<KeyEntry>();
            keyEntries.Add(new KeyEntry()
            {
                Name = faker.Name.FullName(),
                Meta = new Dictionary<string, string>() {
                    { "key1", "value1" }},
                Value = this.faker.Random.Bytes(2)
            });
            keyEntries.Add(new KeyEntry()
            {
                Name = faker.Name.FullName(),
                Meta = new Dictionary<string, string>() {
                    { "key2", "value2" }},
                Value = this.faker.Random.Bytes(2)
            });
            await syncStorage.StoreEntriesAsync(keyEntries);

            var cloudEntries = cloudStorage.RetrieveAllEntries();

            Assert.Equal(2, cloudEntries.Count);
            AreEqual(keyEntries.First(), cloudEntries.Find((obj) => obj.Name == keyEntries.First().Name));
            AreEqual(keyEntries.Last(), cloudEntries.Find((obj) => obj.Name == keyEntries.Last().Name));

            Assert.Equal(2, localStorage.Names().Length);
            AreEqual(keyEntries.First(), localStorage.Load(keyEntries.First().Name));

            AreEqual(keyEntries.Last(), localStorage.Load(keyEntries.Last().Name));

            await syncStorage.DeleteEntryAsync(keyEntries.First().Name);

            cloudEntries = cloudStorage.RetrieveAllEntries();
            Assert.Equal(1, cloudEntries.Count);
            AreEqual(keyEntries.Last(), cloudEntries.Last());
           
            Assert.Equal(1, localStorage.Names().Length);
            AreEqual(keyEntries.Last(), localStorage.Load(keyEntries.Last().Name));
        }


        [Fact]
        public async Task KTC_32()
        {
            var identity = faker.Random.Guid().ToString();
            var newManager = IntegrationHelper.GetKeyknoxManager(faker.Random.Guid().ToString());
            var cloudStorage = new CloudKeyStorage(newManager);
            var localStorage = new LocalKeyStorage(identity);

            var syncStorage = new SyncKeyStorage(identity, cloudStorage, localStorage);
            await syncStorage.SynchronizeAsync();

            var name = faker.Name.FullName();
            var meta = new Dictionary<string, string>() {{ "key1", "value1" }};
            var value = this.faker.Random.Bytes(2);
          
            await syncStorage.StoreEntryAsync(name, value, meta);

            var cloudEntries = cloudStorage.RetrieveAllEntries();

            Assert.Equal(1, cloudEntries.Count);
            Assert.Equal(value, cloudEntries.First().Data);
            Assert.Equal(meta, cloudEntries.First().Meta);
            Assert.Equal(name, cloudEntries.First().Name);

            Assert.Equal(1, localStorage.Names().Length);
            var localEntry = localStorage.Load(name);

            Assert.Equal(value, localEntry.Value);
            Assert.Equal(name, localEntry.Name);

            await syncStorage.UpdateEntryAsync(name, value, meta);

        }

        [Fact]
        public async Task KTC_33()
        {
            var identity = faker.Random.Guid().ToString();
            var newManager = IntegrationHelper.GetKeyknoxManager(faker.Random.Guid().ToString());
            var cloudStorage = new CloudKeyStorage(newManager);
            var localStorage = new LocalKeyStorage(identity);

            var syncStorage = new SyncKeyStorage(identity, cloudStorage, localStorage);
            await syncStorage.SynchronizeAsync();

            var name = faker.Name.FullName();
            var meta = new Dictionary<string, string>() { { "key1", "value1" } };
            var value = this.faker.Random.Bytes(2);

            await syncStorage.StoreEntryAsync(name, value, meta);
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

            var entry = syncStorage.RetrieveEntry(name);
            Assert.Equal(value, entry.Value);
            Assert.Equal(meta, entry.Meta);
        }

        [Fact]
        public async Task KTC_34()
        {
            var identity = faker.Random.Guid().ToString();
            var newManager = IntegrationHelper.GetKeyknoxManager(faker.Random.Guid().ToString());
            var cloudStorage = new CloudKeyStorage(newManager);
            var localStorage = new LocalKeyStorage(identity);

            var syncStorage = new SyncKeyStorage(identity, cloudStorage, localStorage);
            await syncStorage.SynchronizeAsync();

            var keyEntries = new List<KeyEntry>();
            keyEntries.Add(new KeyEntry()
            {
                Name = faker.Name.FullName(),
                Meta = new Dictionary<string, string>() {
                    { "key1", "value1" }},
                Value = this.faker.Random.Bytes(2)
            });
            keyEntries.Add(new KeyEntry()
            {
                Name = faker.Name.FullName(),
                Meta = new Dictionary<string, string>() {
                    { "key2", "value2" }},
                Value = this.faker.Random.Bytes(2)
            });
            var syncEntries = await syncStorage.StoreEntriesAsync(keyEntries);
            var cloudEntries = cloudStorage.RetrieveAllEntries();

            Assert.Equal(2, syncEntries.Count);
            AreEqual(keyEntries.Find((obj) => obj.Name == syncEntries.First().Name), syncEntries.First());
            AreEqual(keyEntries.Find((obj) => obj.Name == syncEntries.Last().Name), syncEntries.Last());

            Assert.Equal(2, cloudEntries.Count);
            AreEqual(keyEntries.First(), cloudEntries.Find((obj) => obj.Name == keyEntries.First().Name));
            AreEqual(keyEntries.Last(), cloudEntries.Find((obj) => obj.Name == keyEntries.Last().Name));

            Assert.Equal(2, localStorage.Names().Length);
            var localEntry = localStorage.Load(keyEntries.First().Name);
            AreEqual(keyEntries.First(), localEntry);

            var localEntry2 = localStorage.Load(keyEntries.Last().Name);
            AreEqual(keyEntries.Last(), localEntry2);
        }

        [Fact]
        public async Task KTC_35()
        {
            var identity = faker.Random.Guid().ToString();
            var newManager = IntegrationHelper.GetKeyknoxManager(faker.Random.Guid().ToString());
            var cloudStorage = new CloudKeyStorage(newManager);
            var localStorage = new LocalKeyStorage(identity);

            var syncStorage = new SyncKeyStorage(identity, cloudStorage, localStorage);
            await syncStorage.SynchronizeAsync();

            var keyEntries = new List<KeyEntry>();
            keyEntries.Add(new KeyEntry()
            {
                Name = faker.Name.FullName(),
                Meta = new Dictionary<string, string>() {
                    { "key1", "value1" }},
                Value = this.faker.Random.Bytes(2)
            });
            keyEntries.Add(new KeyEntry()
            {
                Name = faker.Name.FullName(),
                Meta = new Dictionary<string, string>() {
                    { "key2", "value2" }},
                Value = this.faker.Random.Bytes(2)
            });

            keyEntries.Add(new KeyEntry()
            {
                Name = faker.Name.FullName(),
                Meta = new Dictionary<string, string>() {
                    { "key3", "value3" }},
                Value = this.faker.Random.Bytes(2)
            });

            var syncEntries = await syncStorage.StoreEntriesAsync(keyEntries);

            await syncStorage.DeleteEntryAsync(keyEntries[0].Name);
            await syncStorage.DeleteEntryAsync(keyEntries[1].Name);
            var cloudEntries = cloudStorage.RetrieveAllEntries();

            Assert.Equal(1, syncStorage.RetrieveAllEntries().Count);

            AreEqual(keyEntries.First(), syncEntries.First());
            Assert.Equal(1, cloudEntries.Count);
            AreEqual(keyEntries.Last(), cloudEntries.First());

            Assert.Equal(1, localStorage.Names().Length);
            var localEntry = localStorage.Load(localStorage.Names().First());
            AreEqual(keyEntries.Last(), localEntry);
        }

        [Fact]
        public async Task KTC_36()
        {
            var identity = faker.Random.Guid().ToString();
            var newManager = IntegrationHelper.GetKeyknoxManager(faker.Random.Guid().ToString());
            var cloudStorage = new CloudKeyStorage(newManager);
            var localStorage = new LocalKeyStorage(identity);

            var syncStorage = new SyncKeyStorage(identity, cloudStorage, localStorage);
            await syncStorage.SynchronizeAsync();

            var keyEntries = new List<KeyEntry>();
            keyEntries.Add(new KeyEntry()
            {
                Name = faker.Name.FullName(),
                Meta = new Dictionary<string, string>() {
                    { "key1", "value1" }},
                Value = this.faker.Random.Bytes(2)
            });
            keyEntries.Add(new KeyEntry()
            {
                Name = faker.Name.FullName(),
                Meta = new Dictionary<string, string>() {
                    { "key2", "value2" }},
                Value = this.faker.Random.Bytes(2)
            });

            await syncStorage.StoreEntriesAsync(keyEntries);

            localStorage.Delete(keyEntries[0].Name);
            localStorage.Delete(keyEntries[1].Name);

            localStorage.Store(keyEntries[0]);
            localStorage.Store(keyEntries[1]);

            var syncEntries = syncStorage.RetrieveAllEntries();


            Assert.Equal(2, syncEntries.Count);
            AreEqual(keyEntries.First(), syncEntries.Find((obj) => obj.Name == keyEntries.First().Name));
            AreEqual(keyEntries.Last(), syncEntries.Find((obj) => obj.Name == keyEntries.Last().Name));
        }


        [Fact]
        public async Task KTC_37()
        {
            var identity = faker.Random.Guid().ToString();
            var newManager = IntegrationHelper.GetKeyknoxManager(faker.Random.Guid().ToString());
            var cloudStorage = new CloudKeyStorage(newManager);
            var localStorage = new LocalKeyStorage(identity);

            var syncStorage = new SyncKeyStorage(identity, cloudStorage, localStorage);
            await syncStorage.SynchronizeAsync();

            var keyEntries = new List<KeyEntry>();
            keyEntries.Add(new KeyEntry()
            {
                Name = faker.Name.FullName(),
                Meta = new Dictionary<string, string>() {
                    { "key1", "value1" }},
                Value = this.faker.Random.Bytes(2)
            });
            keyEntries.Add(new KeyEntry()
            {
                Name = faker.Name.FullName(),
                Meta = new Dictionary<string, string>() {
                    { "key2", "value2" }},
                Value = this.faker.Random.Bytes(2)
            });

            await syncStorage.StoreEntriesAsync(keyEntries);

            Assert.Equal(2, syncStorage.RetrieveAllEntries().Count);

            await syncStorage.DeleteAllEntriesAsync();


            Assert.Empty(syncStorage.RetrieveAllEntries());
            Assert.Empty(localStorage.Names());
            Assert.Empty(cloudStorage.RetrieveAllEntries());
        }

        [Fact]
        public async Task KTC_38()
        {
            var identity = faker.Random.Guid().ToString();
            var newManager = IntegrationHelper.GetKeyknoxManager(faker.Random.Guid().ToString());
            var cloudStorage = new CloudKeyStorage(newManager);
            var localStorage = new LocalKeyStorage(identity);

            var syncStorage = new SyncKeyStorage(identity, cloudStorage, localStorage);
            await syncStorage.SynchronizeAsync();

            await syncStorage.DeleteAllEntriesAsync();

            Assert.Empty(syncStorage.RetrieveAllEntries());
            Assert.Empty(localStorage.Names());
            Assert.Empty(cloudStorage.RetrieveAllEntries());
        }

        [Fact]
        public async Task KTC_39()
        {
            var identity = faker.Random.Guid().ToString();
            var newManager = IntegrationHelper.GetKeyknoxManager(faker.Random.Guid().ToString());
            var cloudStorage = new CloudKeyStorage(newManager);
            var localStorage = new LocalKeyStorage(identity);

            var syncStorage = new SyncKeyStorage(identity, cloudStorage, localStorage);
            await syncStorage.SynchronizeAsync();

            var name = faker.Name.FullName();
            var meta = new Dictionary<string, string>() {
                { "key1", "value1" }};

            var data = this.faker.Random.Bytes(2);

            await syncStorage.StoreEntryAsync(name, data, meta);
            Assert.NotEmpty(syncStorage.RetrieveAllEntries());
            Assert.NotEmpty(cloudStorage.RetrieveAllEntries());
            Assert.NotEmpty(localStorage.Names());

            var exception = Record.Exception(() =>
            {
               syncStorage.RetrieveEntry("missing entry name");
            });
            Assert.IsType<Virgil.SDK.KeyNotFoundException>(exception);
           
            exception = Record.Exception(() =>
            {
                cloudStorage.RetrieveEntry("missing entry name");
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
            var identity = faker.Random.Guid().ToString();
            var newManager = IntegrationHelper.GetKeyknoxManager(faker.Random.Guid().ToString());
            var cloudStorage = new CloudKeyStorage(newManager);
            var localStorage = new LocalKeyStorage(identity);

            var syncStorage = new SyncKeyStorage(identity, cloudStorage, localStorage);
            await syncStorage.SynchronizeAsync();

            var keyEntry = new KeyEntry()
            {
                Name = faker.Name.FullName(),
                Meta = new Dictionary<string, string>() {
                    { "key1", "value1" }},
                Value = this.faker.Random.Bytes(2)
            };

            localStorage.Store(keyEntry);

            var exception = Record.ExceptionAsync(async () =>
            {
                await syncStorage.DeleteEntryAsync(keyEntry.Name);
            });


            Assert.IsType<MissingEntryException>(await exception);


            exception = Record.ExceptionAsync(async () =>
            {
                await syncStorage.DeleteEntriesAsync(new List<string>() { keyEntry.Name });
            });
            Assert.IsType<MissingEntryException>(await exception);


            exception = Record.ExceptionAsync(async () =>
            {
                await syncStorage.StoreEntryAsync(keyEntry.Name, keyEntry.Value, keyEntry.Meta);
            });
            Assert.IsType<NonUniqueEntryException>(await exception);

            exception = Record.ExceptionAsync(async () =>
            {
                await syncStorage.StoreEntriesAsync(new List<KeyEntry>(){keyEntry});
            });
            Assert.IsType<NonUniqueEntryException>(await exception);


            Assert.True(syncStorage.ExistsEntry(keyEntry.Name));

            Assert.NotNull(syncStorage.RetrieveEntry(keyEntry.Name));

            Assert.NotNull(syncStorage.RetrieveAllEntries());

            exception = Record.ExceptionAsync(async () =>
            {
                await syncStorage.UpdateEntryAsync(keyEntry.Name, keyEntry.Value, keyEntry.Meta);
            });
            Assert.IsType<MissingEntryException>(await exception);
        }

        private void AreEqual(KeyEntry expected, KeyEntry actual){
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
