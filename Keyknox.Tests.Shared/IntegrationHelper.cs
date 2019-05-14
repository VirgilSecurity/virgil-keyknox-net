namespace Keyknox.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Bogus;
    using Keyknox.Client;
    using Virgil.Crypto;
    using Virgil.CryptoAPI;
    using Virgil.SDK.Common;
    using Virgil.SDK.Web.Authorization;

    public class IntegrationHelper
    {
        private static VirgilCrypto crypto = new VirgilCrypto();
        private static ServiceTestData serviceTestData = new ServiceTestData("keyknox-default");
        private static Faker faker = new Faker();

        public static IPrivateKey ApiPrivateKey()
        {
            return crypto.ImportPrivateKey(
                Bytes.FromString(serviceTestData.ApiPrivateKey, StringEncoding.BASE64));
        }

        public static Func<TokenContext, Task<string>> GetObtainToken(string defaultIdentity = null, double lifeTimeMin = 10)
        {
            Func<TokenContext, Task<string>> obtainToken = async (TokenContext tokenContext) =>
            {
                var jwtFromServer = await EmulateServerResponseToBuildTokenRequest(tokenContext, defaultIdentity, lifeTimeMin);

                return jwtFromServer;
            };

            return obtainToken;
        }

        public static Task<string> EmulateServerResponseToBuildTokenRequest(TokenContext tokenContext, string defaultIdentity, double lifeTimeMin = 5)
        {
            var serverResponse = Task<string>.Factory.StartNew(() =>
            {
                // Thread.Sleep(1000); // simulation of long-term processing
                var data = new Dictionary<object, object>
                    {
                        { "username", "my_username" }
                    };
                var builder = new JwtGenerator(
                    serviceTestData.AppId,
                    ApiPrivateKey(),
                    serviceTestData.ApiPublicKeyId,
                    TimeSpan.FromMinutes(lifeTimeMin),
                    new VirgilAccessTokenSigner());
                var identity = string.IsNullOrWhiteSpace(tokenContext.Identity) ? defaultIdentity : tokenContext.Identity;
                return builder.GenerateToken(identity, data).ToString();
            });
            return serverResponse;
        }

        public static KeyknoxManager GetKeyknoxManager(IPrivateKey privateKey, IPublicKey[] publicKeys, string identity)
        {
            var callBackProvider = new CallbackJwtProvider(GetObtainToken(identity));
            var manager = new KeyknoxManager(
                callBackProvider,
                privateKey,
                publicKeys,
                new KeyknoxClient(new NewtonsoftJsonExtendedSerializer(), serviceTestData.ServiceAddress));
            return manager;
        }

        public static LocalKeyStorage GetLocalKeyStorage(string identity)
        {
#if OSX
            return new LocalKeyStorage(identity);
#else
            return new LocalKeyStorage(identity, 'some password');
#endif
        }

        public static KeyknoxManager GetKeyknoxManager(string identity)
        {
            var keypair = crypto.GenerateKeys();
            var keypair2 = crypto.GenerateKeys();
            return GetKeyknoxManager(
                keypair.PrivateKey,
                new IPublicKey[] { keypair.PublicKey, keypair2.PublicKey },
                identity);
        }

        public static TokenContext GetTokenContext(string identity)
        {
            return new TokenContext(identity, "get", false, "keyknox");
        }

        private static string SomeHash(string identity)
        {
            return string.IsNullOrWhiteSpace(identity) ? faker.Random.Guid().ToString() : identity;
        }
    }
}
