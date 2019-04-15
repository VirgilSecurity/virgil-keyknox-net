using System.Threading.Tasks;
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
        public async Task KTC_1_PullValue()
        {
            var identity = faker.Random.String(10);
            var token = await IntegrationHelper.GetObtainToken().Invoke(IntegrationHelper.GetTokenContext(identity));
            var meta = this.faker.Random.Bytes(5);
            var data = this.faker.Random.Bytes(10);

            var response = await this.client.PushValueAsync(meta, data, null, token);
            Assert.Equal(meta, response.Meta);
            Assert.Equal(data, response.Value);
            Assert.Equal("1.0", response.Version);
            Assert.NotNull(response.KeyknoxHash);

            var pullSesponse = await client.PullValueAsync(token);
            Assert.Equal(meta, pullSesponse.Meta);
            Assert.Equal(data, pullSesponse.Value);
            Assert.Equal("1.0", pullSesponse.Version);
            Assert.Equal(response.KeyknoxHash, pullSesponse.KeyknoxHash);


             var resetResponse = await this.client.ResetValueAsync(token);
             Assert.Empty(resetResponse.Meta);
             Assert.Empty(resetResponse.Value);
             Assert.Equal("2.0", resetResponse.Version);
             Assert.NotNull(resetResponse.KeyknoxHash);
        }

        [Fact]
        public async Task KTC_3_PullEmptyValue()
        {
            var identity = faker.Random.String(10);
            var token = await IntegrationHelper.GetObtainToken().Invoke(IntegrationHelper.GetTokenContext(identity));

            var pullSesponse = await client.PullValueAsync(token);
            Assert.Null(pullSesponse.Meta);
            Assert.Null(pullSesponse.Value);
            Assert.Null(pullSesponse.KeyknoxHash);
            Assert.Equal("1.0", pullSesponse.Version);

        }

        [Fact]
        public async Task KTC_4_ResetValue()
        {
            var identity = faker.Random.String(10);
            var token = await IntegrationHelper.GetObtainToken().Invoke(IntegrationHelper.GetTokenContext(identity));
            var meta = this.faker.Random.Bytes(5);
            var data = this.faker.Random.Bytes(10);

            var response = await this.client.PushValueAsync(meta, data, null, token);
            Assert.Equal(meta, response.Meta);
            Assert.Equal(data, response.Value);
            Assert.Equal("1.0", response.Version);
            Assert.NotNull(response.KeyknoxHash);

            var resetResponse = await this.client.ResetValueAsync(token);
            Assert.Empty(resetResponse.Meta);
            Assert.Empty(resetResponse.Value);
            Assert.Equal("2.0", resetResponse.Version);
            Assert.NotNull(resetResponse.KeyknoxHash);
        }

        [Fact]
        public async Task KTC_5_ResetEmptyValue()
        {
            var identity = faker.Random.String(10);
            var token = await IntegrationHelper.GetObtainToken().Invoke(IntegrationHelper.GetTokenContext(identity));

            var resetResponse = await this.client.ResetValueAsync(token);
            Assert.Empty(resetResponse.Meta);
            Assert.Empty(resetResponse.Value);
            Assert.Equal("1.0", resetResponse.Version);
            Assert.NotNull(resetResponse.KeyknoxHash);
        }

        [Fact]
        public async Task KTC_2_UpdateValue()
        {
            var identity = faker.Random.String(10);
            var token = await IntegrationHelper.GetObtainToken().Invoke(IntegrationHelper.GetTokenContext(identity));
            var meta = this.faker.Random.Bytes(5);
            var data = this.faker.Random.Bytes(10);

            var response = await this.client.PushValueAsync(meta, data, null, token);
            Assert.Equal(meta, response.Meta);
            Assert.Equal(data, response.Value);
            Assert.Equal("1.0", response.Version);
            Assert.NotNull(response.KeyknoxHash);

            var response2 = await this.client.PushValueAsync(meta, data, response.KeyknoxHash, token);
            Assert.Equal(response.Meta, response2.Meta);
            Assert.Equal(response.Value, response2.Value);
            Assert.Equal("2.0", response2.Version);
            Assert.NotNull(response.KeyknoxHash);

            var resetResponse = await this.client.ResetValueAsync(token);
        }
    }
}
