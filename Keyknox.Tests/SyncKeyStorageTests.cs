using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bogus;
using Virgil.SDK;
using Xunit;

namespace Keyknox.Tests
{
    public class SyncKeyStorageTests
    {
        Faker faker = new Faker();

        public SyncKeyStorageTests()
        {
        }


        [Fact]
        public async Task KTC_29_Store()
        { 
            var identity = faker.Random.Guid().ToString();
            var newManager = IntegrationHelper.GetKeyknoxManager(faker.Random.Guid().ToString());
            var cloudStorage = new CloudKeyStorage(newManager);
            var localStorage = new KeyStorage();
            var syncStorage = new SyncKeyStorage(identity, cloudStorage, localStorage);

            await syncStorage.SynchronizeAsync();
            Assert.Empty(syncStorage.RetrieveAllEntries());
            var data = this.faker.Random.Bytes(10);
            var name = faker.Random.Guid().ToString();

           // await syncStorage.StoreEntryAsync(name, data, new Dictionary<string, string>() { { "key1", "value1" });
         
        }
    }
}
