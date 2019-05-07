namespace Keyknox.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Bogus;
    using Keyknox.Client;
    using Virgil.Crypto;
    using Virgil.CryptoAPI;
    using Virgil.SDK.Common;
    using Xunit;

    public class KeyknoxManagerTests
    {
        private Faker faker = new Faker();
        private KeyknoxManager manager;
        private VirgilCrypto crypto;
        private string defaultIdentity;

        public KeyknoxManagerTests()
        {
            this.crypto = new VirgilCrypto();
            this.defaultIdentity = this.faker.Random.Guid().ToString();
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
            Assert.NotEmpty(pushedVal.Meta);
            Assert.Equal(pushedVal.Meta, pulled.Meta);
        }

        [Fact]
        public async Task KTC_8_PullEmptyValue()
        {
            var data = this.faker.Random.Bytes(10);
            var newManager = IntegrationHelper.GetKeyknoxManager(this.faker.Random.Guid().ToString());
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
                keyPairs[i] = this.crypto.GenerateKeys();
            }

            var prevPart = keyPairs.SkipLast(25).ToArray().Select((arg) => arg.PublicKey).ToArray();
            var lastPart = keyPairs.Skip(25).ToArray().Select((arg) => arg.PublicKey).ToArray();
            var data = this.faker.Random.Bytes(10);
            var identity = this.faker.Random.Guid().ToString();
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

            var ex = Record.ExceptionAsync(async () =>
            {
                await newManager2.PullValueAsync();
            });
            Assert.IsType<VirgilCryptoException>(await ex);
        }

        [Fact]
        public async Task KTC_10_PullValueWithSignerInTrustedPubKeys()
        {
            var keyPairs = new KeyPair[50];
            for (var i = 0; i < 50; i++)
            {
                keyPairs[i] = this.crypto.GenerateKeys();
            }

            var prevPart = keyPairs.SkipLast(25).ToArray().Select((arg) => arg.PublicKey).ToArray();
            var lastPart = keyPairs.Skip(25).ToArray().Select((arg) => arg.PublicKey).ToArray();

            Random rand = new Random();

            var data = this.faker.Random.Bytes(10);
            var identity = this.faker.Random.Guid().ToString();
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

            var ex = Record.ExceptionAsync(async () =>
            {
                await newManager3.PullValueAsync();
            });
            Assert.IsType<VirgilCryptoException>(await ex);
        }

        [Fact]
        public async Task KTC_11_UpdateRecipients()
        {
            var keyPairs = new KeyPair[50];
            for (var i = 0; i < 50; i++)
            {
                keyPairs[i] = this.crypto.GenerateKeys();
            }

            var prevPart = keyPairs.SkipLast(25).ToArray().Select((arg) => arg.PublicKey).ToArray();
            var lastPart = keyPairs.Skip(25).ToArray().Select((arg) => arg.PublicKey).ToArray();

            Random rand = new Random();

            var data = this.faker.Random.Bytes(10);
            var identity = this.faker.Random.Guid().ToString();
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
           Assert.NotEqual(pulledVal.Meta, reencryptedData.Meta);

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

            var ex = Record.ExceptionAsync(async () =>
            {
                await newManager4.PullValueAsync();
            });
            Assert.IsType<VirgilCryptoException>(await ex);

            var newManager5 = IntegrationHelper.GetKeyknoxManager(
              keyPairs[rand.Next(25, 50)].PrivateKey,
              prevPart,
              identity);

             ex = Record.ExceptionAsync(async () =>
            {
                await newManager5.PullValueAsync();
            });
            Assert.IsType<VirgilCryptoException>(await ex);
        }

        [Fact]
        public async Task KTC_12_UpdateRecipientsWithNewData()
        {
            var keyPairs = new KeyPair[50];
            for (var i = 0; i < 50; i++)
            {
                keyPairs[i] = this.crypto.GenerateKeys();
            }

            var prevPart = keyPairs.SkipLast(25).ToArray().Select((arg) => arg.PublicKey).ToArray();
            var lastPart = keyPairs.Skip(25).ToArray().Select((arg) => arg.PublicKey).ToArray();

            Random rand = new Random();

            var data = this.faker.Random.Bytes(10);
            var identity = this.faker.Random.Guid().ToString();
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

            var updatedData = this.faker.Random.Bytes(10);
            var reencryptedData = await newManager2.UpdateRecipientsAsync(
                updatedData,
                pulledVal.KeyknoxHash,
                lastPart,
                keyPairs[rand.Next(25, 50)].PrivateKey);
            Assert.Equal(updatedData, reencryptedData.Value);
            Assert.NotEqual(pulledVal.Meta, reencryptedData.Meta);

            var newManager3 = IntegrationHelper.GetKeyknoxManager(
                keyPairs[rand.Next(25, 50)].PrivateKey,
                lastPart,
                identity);

            var pulledData = await newManager3.PullValueAsync();
            Assert.Equal(updatedData, pulledData.Value);

            var newManager4 = IntegrationHelper.GetKeyknoxManager(
               keyPairs[rand.Next(25)].PrivateKey,
               lastPart,
               identity);

            var ex = Record.ExceptionAsync(async () =>
            {
                await newManager4.PullValueAsync();
            });
            Assert.IsType<VirgilCryptoException>(await ex);

            var newManager5 = IntegrationHelper.GetKeyknoxManager(
              keyPairs[rand.Next(25, 50)].PrivateKey,
              prevPart,
              identity);

            ex = Record.ExceptionAsync(async () =>
            {
                await newManager5.PullValueAsync();
            });
            Assert.IsType<VirgilCryptoException>(await ex);
        }

        [Fact]
        public async Task KTC_13_UpdateRecipientsWithEmptyData()
        {
            var keyPairs = new KeyPair[50];
            for (var i = 0; i < 50; i++)
            {
                keyPairs[i] = this.crypto.GenerateKeys();
            }

            var prevPart = keyPairs.SkipLast(25).ToArray().Select((arg) => arg.PublicKey).ToArray();
            var lastPart = keyPairs.Skip(25).ToArray().Select((arg) => arg.PublicKey).ToArray();

            Random rand = new Random();

            var data = this.faker.Random.Bytes(10);
            var identity = this.faker.Random.Guid().ToString();
            var newManager = IntegrationHelper.GetKeyknoxManager(
                keyPairs[rand.Next(25)].PrivateKey,
                prevPart,
                identity);

            var reencryptedData = await newManager.UpdateRecipientsAsync(
                lastPart,
                keyPairs[rand.Next(25)].PrivateKey);
            Assert.Empty(reencryptedData.Value);
            Assert.Empty(reencryptedData.Meta);
            Assert.Equal("1.0", reencryptedData.Version);
        }

        [Fact]
        public async Task KTC_14_ResetValue()
        {
            var data = this.faker.Random.Bytes(10);
            var decryptedVal = await this.manager.PushValueAsync(data);
            Assert.NotEmpty(decryptedVal.Value);
            Assert.NotEmpty(decryptedVal.Meta);
            Assert.Equal("1.0", decryptedVal.Version);
            var resetedVal = await this.manager.ResetValueAsync();
            Assert.Equal("2.0", resetedVal.Version);
            Assert.Empty(resetedVal.Value);
            Assert.Empty(resetedVal.Meta);
        }

        [Fact]
        public async Task KTC_15_ResetInvalidValue()
        {
            var keyPair1 = this.crypto.GenerateKeys();
            var keyPair2 = this.crypto.GenerateKeys();
            var identity = this.faker.Random.Guid().ToString();

            var newManager = IntegrationHelper.GetKeyknoxManager(
                keyPair1.PrivateKey,
                new IPublicKey[] { keyPair1.PublicKey },
                identity);

            var data = this.faker.Random.Bytes(10);
            var pushedVal = await newManager.PushValueAsync(data);
            Assert.Equal(data, pushedVal.Value);

            var newManager2 = IntegrationHelper.GetKeyknoxManager(
                keyPair2.PrivateKey,
                new IPublicKey[] { keyPair2.PublicKey },
                identity);

            var resetedVal = await newManager2.ResetValueAsync();
            Assert.Empty(resetedVal.Value);
            Assert.Empty(resetedVal.Meta);
            Assert.Equal("2.0", resetedVal.Version);

            var pulled = await newManager.PullValueAsync();
        }

        [Fact]
        public async Task KTC_16_UploadedByManagerValueIsAvailableForClient()
        {
            var keyPair1 = this.crypto.GenerateKeys();
            var identity = this.faker.Random.Guid().ToString();

            var newManager = IntegrationHelper.GetKeyknoxManager(
                keyPair1.PrivateKey,
                new IPublicKey[] { keyPair1.PublicKey },
                identity);

            var data = this.faker.Random.Bytes(10);
            var pushedVal = await newManager.PushValueAsync(data);
            Assert.Equal(data, pushedVal.Value);

            var client = new KeyknoxClient(
                new NewtonsoftJsonExtendedSerializer(),
                new ServiceTestData("keyknox-default").ServiceAddress);

            var token = await IntegrationHelper.GetObtainToken().Invoke(IntegrationHelper.GetTokenContext(identity));
            var pullResponse = await client.PullValueAsync(token);

            Assert.Equal(pushedVal.Meta, pullResponse.Meta);
            Assert.Equal(pushedVal.Meta, pullResponse.Meta);
            Assert.Equal(pushedVal.Version, pullResponse.Version);
            Assert.Equal(pushedVal.KeyknoxHash, pullResponse.KeyknoxHash);

            var keyknoxCrypto = new KeyknoxCrypto();
            var decrypted = keyknoxCrypto.Decrypt(
                pullResponse,
                keyPair1.PrivateKey,
                new IPublicKey[] { keyPair1.PublicKey });
            Assert.Equal(pushedVal.Value, decrypted.Value);
        }
    }
}
