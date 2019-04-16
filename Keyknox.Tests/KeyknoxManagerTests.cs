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
        string defaultIdentity;
        public KeyknoxManagerTests()
        {
            this.crypto = new VirgilCrypto();
            this.defaultIdentity = faker.Random.Guid().ToString();
            this.manager = IntegrationHelper.GetKeyknoxManager(this.defaultIdentity);
        }

        [Fact]
        public async Task KTC_6_PushValue()
        {
            var data = this.faker.Random.Bytes(10);
            var decryptedVal = await this.manager.PushValueAsync(data);
            Assert.Equal(data, decryptedVal.Value);
        }

        [Fact]
        public async Task KTC_7_PullValue()
        {
            var data = this.faker.Random.Bytes(10);
            var pushedVal = await this.manager.PushValueAsync(data);

            var pulled = await this.manager.PullValueAsync();
            Assert.Equal(data, pulled.Value);
            Assert.Equal(pushedVal.Meta, pulled.Meta);
        }

        [Fact]
        public async Task KTC_8_PullEmptyValue()
        {
            var data = this.faker.Random.Bytes(10);
            var newManager = IntegrationHelper.GetKeyknoxManager(faker.Random.Guid().ToString());
            var pulled = await newManager.PullValueAsync();
            Assert.Empty(pulled.Value);
            Assert.Equal("1.0", pulled.Version);
        }


        [Fact]
        public async Task KTC_9_PullValueWithoutSignerInTrustedPubKeys()
        {
            var keyPairs = new KeyPair[50];
            for (var i = 0; i < 50; i++)
            {
                keyPairs[i] = crypto.GenerateKeys();
            }
            var prevPart = keyPairs.SkipLast(25).ToArray().Select((arg) => arg.PublicKey).ToArray();
            var lastPart = keyPairs.Skip(25).ToArray().Select((arg) => arg.PublicKey).ToArray();
            var data = this.faker.Random.Bytes(10);
            var identity = faker.Random.Guid().ToString();
            var newManager = IntegrationHelper.GetKeyknoxManager(
                keyPairs[0].PrivateKey,
                prevPart,
                identity);

            var pushedVal = await newManager.PushValueAsync(data);

            var pulledVal = await newManager.PullValueAsync();
            Assert.Equal(pushedVal.Value, pulledVal.Value);

            var newManager2 = IntegrationHelper.GetKeyknoxManager(
               keyPairs[0].PrivateKey,
                lastPart,
                identity);

            await AssertException(newManager2);
        }

        private static async Task AssertException(KeyknoxManager newManager2)
        {
            //it doesnt work
            //var ex = Record.ExceptionAsync(async () =>
            //{
            //    await newManager.PullValueAsync();
            //});
            //Assert.IsType<VirgilCryptoException>(ex);

            var virgilCryptoExceptionRaised = false;
            try
            {
                await newManager2.PullValueAsync();
            }
            catch (Exception exception)
            {
                Assert.IsType<VirgilCryptoException>(exception);
                virgilCryptoExceptionRaised = true;
            }
            Assert.True(virgilCryptoExceptionRaised);
        }

        [Fact]
        public async Task KTC_10_PullValueWithSignerInTrustedPubKeys()
        {
            var keyPairs = new KeyPair[50];
            for (var i = 0; i < 50; i++)
            {
                keyPairs[i] = crypto.GenerateKeys();
            }
            var prevPart = keyPairs.SkipLast(25).ToArray().Select((arg) => arg.PublicKey).ToArray();
            var lastPart = keyPairs.Skip(25).ToArray().Select((arg) => arg.PublicKey).ToArray();


            Random rand = new Random();

            var data = this.faker.Random.Bytes(10);
            var identity = faker.Random.Guid().ToString();
            var newManager = IntegrationHelper.GetKeyknoxManager(
                keyPairs[rand.Next(25)].PrivateKey,
                prevPart,
                identity);

            var pushedVal = await newManager.PushValueAsync(data);
           
            Assert.Equal(data, pushedVal.Value);

            var newManager2 = IntegrationHelper.GetKeyknoxManager(
                keyPairs[rand.Next(25)].PrivateKey,
                prevPart,
                identity);

            var pulledVal = await newManager2.PullValueAsync();
            Assert.Equal(data, pulledVal.Value);
            var newManager3 = IntegrationHelper.GetKeyknoxManager(
                keyPairs[rand.Next(25, 50)].PrivateKey,
               prevPart,
               identity);


            var virgilCryptoExceptionRaised = false;
            try
            {
                await newManager3.PullValueAsync();
            }
            catch (Exception exception)
            {
                Assert.IsType<VirgilCryptoException>(exception);
                virgilCryptoExceptionRaised = true;
            }
            Assert.True(virgilCryptoExceptionRaised);
        }

        [Fact]
        public async Task KTC_11_PullValueWithoutSignerInTrustedPubKeys()
        {
            var keyPairs = new KeyPair[50];
            for (var i = 0; i < 50; i++)
            {
                keyPairs[i] = crypto.GenerateKeys();
            }
            var prevPart = keyPairs.SkipLast(25).ToArray().Select((arg) => arg.PublicKey).ToArray();
            var lastPart = keyPairs.Skip(25).ToArray().Select((arg) => arg.PublicKey).ToArray();


            Random rand = new Random();

            var data = this.faker.Random.Bytes(10);
            var identity = faker.Random.Guid().ToString();
            var newManager = IntegrationHelper.GetKeyknoxManager(
                keyPairs[rand.Next(25)].PrivateKey,
                prevPart,
                identity);

            var pushedVal = await newManager.PushValueAsync(data);

            Assert.Equal(data, pushedVal.Value);

            var privKey = keyPairs[rand.Next(25)].PrivateKey;
            var newManager2 = IntegrationHelper.GetKeyknoxManager(
                privKey,
                prevPart,
                identity);

            var pulledVal = await newManager2.PullValueAsync();
            Assert.Equal(data, pulledVal.Value);

            var reencryptedData = await newManager2.UpdateRecipientsAsync(
                lastPart, keyPairs[rand.Next(25, 50)].PrivateKey);
            Assert.Equal(data, reencryptedData.Value);
           // todo? Assert.NotEqual(pulledVal.Meta, reencryptedData.Meta);

            var newManager3 = IntegrationHelper.GetKeyknoxManager(
                keyPairs[rand.Next(25, 50)].PrivateKey,
                lastPart, 
                identity);

            var pulledData = await newManager3.PullValueAsync();
            Assert.Equal(data, pulledData.Value);

            var newManager4 = IntegrationHelper.GetKeyknoxManager(
               keyPairs[rand.Next(25)].PrivateKey,
               lastPart,
               identity);

            var virgilCryptoExceptionRaised = false;
            try
            {
                await newManager4.PullValueAsync();
            }
            catch (Exception exception)
            {
                Assert.IsType<VirgilCryptoException>(exception);
                virgilCryptoExceptionRaised = true;
            }
            Assert.True(virgilCryptoExceptionRaised);

            var newManager5 = IntegrationHelper.GetKeyknoxManager(
              keyPairs[rand.Next(25, 50)].PrivateKey,
              prevPart,
              identity);

            virgilCryptoExceptionRaised = false;
            try
            {
                await newManager5.PullValueAsync();
            }
            catch (Exception exception)
            {
                var ee = exception.Message;
                Assert.IsType<VirgilCryptoException>(exception);
                virgilCryptoExceptionRaised = true;
            }
            Assert.True(virgilCryptoExceptionRaised);
        }

        [Fact]
        public async Task KTC_12_PullValueWithoutSignerInTrustedPubKeys()
        {
            var keyPairs = new KeyPair[50];
            for (var i = 0; i < 50; i++)
            {
                keyPairs[i] = crypto.GenerateKeys();
            }
            var prevPart = keyPairs.SkipLast(25).ToArray().Select((arg) => arg.PublicKey).ToArray();
            var lastPart = keyPairs.Skip(25).ToArray().Select((arg) => arg.PublicKey).ToArray();
            var data = this.faker.Random.Bytes(10);
            var identity = faker.Random.Guid().ToString();
            var newManager = IntegrationHelper.GetKeyknoxManager(
                keyPairs[0].PrivateKey,
                prevPart,
                identity);

            var pushedVal = await newManager.PushValueAsync(data);

            var pulledVal = await newManager.PullValueAsync();
            Assert.Equal(pushedVal.Value, pulledVal.Value);

            var newManager2 = IntegrationHelper.GetKeyknoxManager(
               keyPairs[0].PrivateKey,
                lastPart,
                identity);

            var virgilCryptoExceptionRaised = false;
            try
            {
                await newManager2.PullValueAsync();
            }
            catch (Exception exception)
            {
                Assert.IsType<VirgilCryptoException>(exception);
                virgilCryptoExceptionRaised = true;
            }
            Assert.True(virgilCryptoExceptionRaised);
            //it doesnt work
            //var ex = Record.ExceptionAsync(async () =>
            //{
            //    await newManager.PullValueAsync();
            //});
            //Assert.IsType<VirgilCryptoException>(ex);
        }
    }
}
