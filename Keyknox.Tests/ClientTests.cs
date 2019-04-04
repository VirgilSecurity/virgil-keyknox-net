using System;
using Bogus;
using Keyknox.Client;
using Virgil.SDK.Common;
using Xunit;

namespace Keyknox.Tests
{
    public class ClientTests
    {
        KeyknoxCrypto keyknoxCrypto;
        Faker faker;
        public ClientTests()
        {
            this.keyknoxCrypto = new KeyknoxCrypto();
            this.faker = new Faker();
        }

        [Fact]
        public void Encrypt_Empty_PrivateKey_RaiseException()
        {

            var meta = this.faker.Random.Bytes(5);
            var data = this.faker.Random.Bytes(10);

            var client = new KeyknoxClient(new NewtonsoftJsonSerializer(), "https://api-stg.virgilsecurity.com/");
            client.PushValueAsync(meta, data, null);
            var ex = Record.Exception(() =>
            {
                keyknoxCrypto.Encrypt(data, null, new[] { this.keyPair.PublicKey });
            });

            Assert.IsType<KeyknoxException>(ex);
        }
    }
}
