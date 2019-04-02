using System;
using Bogus;
using Virgil.Crypto;
using Virgil.CryptoAPI;
using Xunit;

namespace Keyknox.Tests
{
    public class KeyknoxCryptoTests
    {
        KeyknoxCrypto keyknoxCrypto;
        KeyPair keyPair;
        Faker faker;
        public KeyknoxCryptoTests(){
            var crypto = new VirgilCrypto();
            this.keyPair = crypto.GenerateKeys();
            this.keyknoxCrypto = new KeyknoxCrypto();
            this.faker = new Faker();
        }
        [Fact]
        public void Encrypt_Empty_PrivateKey_RaiseException()
        {
            
            var data = this.faker.Random.Bytes(5);
           
            var ex = Record.Exception(() =>
            {
                keyknoxCrypto.Encrypt(data, null, new[] { this.keyPair.PublicKey });
            });

            Assert.IsType<KeyknoxException>(ex);
        }

        [Fact]
        public void Encrypt_Null_PublicKey_RaiseException()
        {
            var data = this.faker.Random.Bytes(5);

            var ex = Record.Exception(() =>
            {
                keyknoxCrypto.Encrypt(data, keyPair.PrivateKey, null);
            });

            Assert.IsType<KeyknoxException>(ex);
        }

        [Fact]
        public void Encrypt_Empty_PublicKey_RaiseException()
        {
            var data = this.faker.Random.Bytes(5);

            var ex = Record.Exception(() =>
            {
                keyknoxCrypto.Encrypt(data, keyPair.PrivateKey, new IPublicKey[]{});
            });

            Assert.IsType<KeyknoxException>(ex);
        }

        [Fact]
        public void DecryptThenVerifyDetached_Returns_OriginalData()
        {
            var crypto = new VirgilCrypto();
            var recipientKeys = crypto.GenerateKeys();
            var data = this.faker.Random.Bytes(5);
            var encrypted = crypto.SignThenEncryptDetached(data, keyPair.PrivateKey, new PublicKey[] { recipientKeys.PublicKey });
            Assert.NotNull(encrypted.Value);
            Assert.NotEmpty(encrypted.Value);
            Assert.NotNull(encrypted.Meta);
            Assert.NotEmpty(encrypted.Meta);
            var decryptedData = crypto.DecryptThenVerifyDetached(
                encrypted.Value, 
                encrypted.Meta,
                recipientKeys.PrivateKey,
                new PublicKey[] { keyPair.PublicKey });
            Assert.Equal(data, decryptedData);
        }
    }
}
