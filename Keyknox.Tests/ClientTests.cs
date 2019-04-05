using System;
using System.Threading.Tasks;
using Bogus;
using Keyknox.Client;
using Virgil.SDK.Common;
using Virgil.SDK.Web.Authorization;
using Xunit;

namespace Keyknox.Tests
{
    public class ClientTests
    {
        KeyknoxCrypto keyknoxCrypto;
        Faker faker;
        ServiceTestData serviceTestData;
        KeyknoxClient client;
        public ClientTests()
        {
            this.keyknoxCrypto = new KeyknoxCrypto();
            this.faker = new Faker();
            this.serviceTestData = new ServiceTestData("keyknox-stg");
            this.client = new KeyknoxClient(new NewtonsoftJsonSerializer(), this.serviceTestData.ServiceAddress);
        }

        [Fact]
        public async Task Encrypt_Empty_PrivateKey_RaiseException()
        {
            //KTC-1

            var token = await IntegrationHelper.GetObtainToken().Invoke(IntegrationHelper.PullTokenContext());

            var resetResponse = await this.client.ResetValueAsync(token);
            Assert.Empty(resetResponse.Meta);
            Assert.Empty(resetResponse.Value);
            Assert.Equal("2.0", resetResponse.Version);
            Assert.NotNull(resetResponse.KeyknoxHash);

            var response = await this.client.PullValueAsync(token);

            Assert.Empty(response.Meta);
            Assert.Empty(response.Value);
            Assert.Equal("1.0", response.Version);
            Assert.NotNull(response.KeyknoxHash);

            var meta = this.faker.Random.Bytes(5);
            var data = this.faker.Random.Bytes(10);
            var previousHash = response.KeyknoxHash;


            response = await this.client.PushValueAsync(meta, data, previousHash, token);
            Assert.Equal(meta, response.Meta);
            Assert.Equal(data, response.Value);
            Assert.Equal("1.0", response.Version);
            Assert.NotNull(response.KeyknoxHash);

            response = await client.PullValueAsync(token);
            Assert.Equal(meta, response.Meta);
            Assert.Equal(data, response.Value);
            Assert.Equal("1.0", response.Version);
            Assert.Equal(response.KeyknoxHash, response.KeyknoxHash);
        }

    }
}
