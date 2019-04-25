using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bogus;
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
        public async Task KTC_29_Synchronize()
        { 
            var identity = faker.Random.Guid().ToString();
            var newManager = IntegrationHelper.GetKeyknoxManager(faker.Random.Guid().ToString());
            var cloudStorage = new CloudKeyStorage(newManager);
            var localStorage = new LocalKeyStorage(identity);

          //  var callBackProvider = new CallbackJwtProvider(IntegrationHelper.GetObtainToken(identity));

            var crypto = new VirgilCrypto();
            var keypair = crypto.GenerateKeys();
            var keypair2 = crypto.GenerateKeys();

            var syncStorage = new SyncKeyStorage(identity, cloudStorage, localStorage);
            await syncStorage.SynchronizeAsync();

            Assert.Empty(syncStorage.RetrieveAllEntries());

            var name = faker.Name.FullName();
            var meta = new Dictionary<string, string>() {
                { "key1", "value1" },
                { "key2", "value2" }};

            var data = this.faker.Random.Bytes(10);
            var entry = await cloudStorage.StoreAsync(name, data, meta);

            await syncStorage.SynchronizeAsync();

            var keyEntries = syncStorage.RetrieveAllEntries();
            Assert.Equal(1, keyEntries.Count);
            Assert.Equal(entry.Name, keyEntries.First().Name);
            Assert.Equal(entry.Meta, keyEntries.First().Meta);
            Assert.Equal(entry.Data, keyEntries.First().Value);

            var name2 = faker.Name.FullName();
            var meta2 = new Dictionary<string, string>() {
                { "key1", "value1" },
                { "key2", "value2" }};

            var data2 = this.faker.Random.Bytes(10);

            var entry2 = await cloudStorage.StoreAsync(name2, data2, meta2);
            await syncStorage.SynchronizeAsync();
            keyEntries = syncStorage.RetrieveAllEntries();
            Assert.Equal(2, keyEntries.Count);

            Assert.Equal(entry.Name, keyEntries.First().Name);
            Assert.Equal(entry.Meta, keyEntries.First().Meta);
            Assert.Equal(entry.Data, keyEntries.First().Value);

            Assert.Equal(entry2.Name, keyEntries.Last().Name);
            Assert.Equal(entry2.Meta, keyEntries.Last().Meta);
            Assert.Equal(entry2.Data, keyEntries.Last().Value);

            localStorage.Delete(entry.Name);
            localStorage.Delete(entry2.Name);

            await syncStorage.SynchronizeAsync();

            Assert.Equal(2, localStorage.Names().Length);
            var localEntry = localStorage.Load(entry.Name);
            var localEntry2 = localStorage.Load(entry2.Name);

            Assert.Equal(entry.Data, localEntry.Value);
            Assert.Equal(entry.Meta, localEntry.Meta);

            Assert.Equal(entry2.Data, localEntry2.Value);
            Assert.Equal(entry2.Meta, localEntry2.Meta);

            await cloudStorage.DeteleEntryAsync(entry.Name);

            await syncStorage.SynchronizeAsync();
            Assert.Equal(1, syncStorage.RetrieveAllEntries().Count);

            Assert.Equal(entry2.Name, syncStorage.RetrieveAllEntries().First().Name);
            Assert.Equal(entry2.Data, syncStorage.RetrieveAllEntries().First().Value);
            Assert.Equal(entry2.Meta, syncStorage.RetrieveAllEntries().First().Meta);

            var updatedData = this.faker.Random.Bytes(1);
            await cloudStorage.UpdateEntryAsync(entry2.Name, updatedData);

            await syncStorage.SynchronizeAsync();

            Assert.Equal(1, localStorage.Names().Length);
            localEntry = localStorage.Load(entry2.Name);

            Assert.Equal(updatedData, localEntry.Value);

            await cloudStorage.DeteleAllAsync();

            await syncStorage.SynchronizeAsync();

            Assert.Empty(localStorage.Names());
        }

        [Fact]
        public async Task KTC_30_Synchronize()
        {
            var identity = faker.Random.Guid().ToString();
            var newManager = IntegrationHelper.GetKeyknoxManager(faker.Random.Guid().ToString());
            var cloudStorage = new CloudKeyStorage(newManager);
            var localStorage = new LocalKeyStorage(identity);

            //  var callBackProvider = new CallbackJwtProvider(IntegrationHelper.GetObtainToken(identity));

            var crypto = new VirgilCrypto();
            var keypair = crypto.GenerateKeys();
            var keypair2 = crypto.GenerateKeys();

            var syncStorage = new SyncKeyStorage(identity, cloudStorage, localStorage);
            await syncStorage.SynchronizeAsync();

            var name = faker.Name.FullName();
            var meta = new Dictionary<string, string>() {
                { "key1", "value1" }};

            var data = this.faker.Random.Bytes(2);

            await syncStorage.StoreEntryAsync(name, data, meta);

            var entries = cloudStorage.RetrieveAllEntries();

            Assert.Equal(1, entries.Count);
            Assert.Equal(data, entries.First().Data);
            Assert.Equal(meta, entries.First().Meta);
            Assert.Equal(name, entries.First().Name);


            Assert.Equal(1, localStorage.Names().Length);
            var entry = localStorage.Load(name);
            Assert.Equal(data, entry.Value);
            Assert.Equal(meta, entry.Meta);
        }
    }
}
