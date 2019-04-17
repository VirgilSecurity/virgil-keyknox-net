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

namespace Keyknox.Tests
{
    public class IntegrationHelper
    {
        public static VirgilCrypto Crypto = new VirgilCrypto();
        public static ServiceTestData ServiceTestData = new ServiceTestData("keyknox-default");
        public static Faker Faker = new Faker();
        public static IPrivateKey ApiPrivateKey()
        {
            return Crypto.ImportPrivateKey(
                Bytes.FromString(ServiceTestData.ApiPrivateKey, StringEncoding.BASE64));
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
                //Thread.Sleep(1000); // simulation of long-term processing
                var data = new Dictionary<object, object>
                    {
                        {"username", "my_username"}
                    };
                var builder = new JwtGenerator(
                    ServiceTestData.AppId,
                    ApiPrivateKey(),
                    ServiceTestData.ApiPublicKeyId,
                    TimeSpan.FromMinutes(lifeTimeMin),
                    new VirgilAccessTokenSigner()
                );
                var identity = String.IsNullOrWhiteSpace(tokenContext.Identity) ? defaultIdentity : tokenContext.Identity;
                return builder.GenerateToken(identity, data).ToString();
            }
            );

            return serverResponse;
        }

        public static KeyknoxManager GetKeyknoxManager(IPrivateKey privateKey, IPublicKey[] publicKeys, string identity){
            var callBackProvider = new CallbackJwtProvider(GetObtainToken(identity));
            var manager = new KeyknoxManager(
                callBackProvider, 
                privateKey, 
                publicKeys,
                new KeyknoxClient(new NewtonsoftJsonSerializer(), ServiceTestData.ServiceAddress));
            return manager;
        }

        public static KeyknoxManager GetKeyknoxManager(string identity)
        {
            var keypair = Crypto.GenerateKeys();
            var keypair2 = Crypto.GenerateKeys();
            return GetKeyknoxManager(
                keypair.PrivateKey,
                new IPublicKey[] { keypair.PublicKey, keypair2.PublicKey },
                identity);
        }

        private static string SomeHash(string identity)
        {
            return String.IsNullOrWhiteSpace(identity) ? Faker.Random.Guid().ToString() : identity;
        }

        public static TokenContext GetTokenContext(string identity){
            return new TokenContext(identity, "get", false, "keyknox");
        }
    }
}
