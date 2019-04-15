using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bogus;
using Virgil.Crypto;
using Virgil.CryptoAPI;
using Xunit;

namespace Keyknox.Tests
{
    public class KeyknoxManagerTests
    {
        Faker faker = new Faker();
        KeyknoxManager manager;
        VirgilCrypto crypto;
        public KeyknoxManagerTests()
        {
            this.crypto = new VirgilCrypto();
            this.manager = IntegrationHelper.GetKeyknoxManager();
        }

        [Fact]
        public async Task KTC_6_PushValue(){
            var data = this.faker.Random.Bytes(10);
            var decryptedVal = await this.manager.PushValueAsync(data);
            Assert.Equal(data, decryptedVal.Value);
        }

        [Fact]
        public async Task KTC_7_PullValue()
        {
            var data = this.faker.Random.Bytes(10);
            var pushedVal = await this.manager.PushValueAsync(data);

            var pulledVal = await this.manager.PullValueAsync();
            Assert.Equal(pushedVal.Value, pulledVal.Value);
        }

        [Fact]
        public async Task KTC_8_PullEmptyValue()
        {
            var data = this.faker.Random.Bytes(10);
            var newManager = IntegrationHelper.GetKeyknoxManager();
            var resetedVal = await newManager.PullValueAsync();
            Assert.Null(resetedVal.Value);
            Assert.Equal("1.0", resetedVal.Version);
        }


        [Fact]
        public async Task KTC_9_PullEmptyValue()
        {
            var keyPairs = new KeyPair[50];
            for (var i = 0; i < 50; i++){
                keyPairs[i] = crypto.GenerateKeys();
            }
            var data = this.faker.Random.Bytes(10);
            var newManager = IntegrationHelper.GetKeyknoxManager(
                keyPairs[0].PrivateKey,
                keyPairs.SkipLast(25).ToArray().Select((arg) => arg.PublicKey).ToArray());

            var pushedVal = await newManager.PushValueAsync(data);

            var pulledVal = await newManager.PullValueAsync();
            Assert.Equal(pushedVal.Value, pulledVal.Value);

            var newManager2 = IntegrationHelper.GetKeyknoxManager(
               keyPairs[0].PrivateKey,
                keyPairs.Skip(25).ToArray().Select((arg) => arg.PublicKey).ToArray());

            var ex = Record.ExceptionAsync(async () =>
            {
                await newManager2.PullValueAsync();
            });
            Assert.IsType<Exception>(ex);
        }
    }
}
