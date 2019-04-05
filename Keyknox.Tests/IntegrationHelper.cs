using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Virgil.Crypto;
using Virgil.CryptoAPI;
using Virgil.SDK.Common;
using Virgil.SDK.Web.Authorization;

namespace Keyknox.Tests
{
    public class IntegrationHelper
    {
        public static VirgilCrypto Crypto = new VirgilCrypto();
        public static ServiceTestData ServiceTestData = new ServiceTestData("keyknox-stg");

        public static IPrivateKey ApiPrivateKey()
        {
            return Crypto.ImportPrivateKey(
                Bytes.FromString(ServiceTestData.ApiPrivateKey, StringEncoding.BASE64));
        }

        public static Func<TokenContext, Task<string>> GetObtainToken(double lifeTimeMin = 10)
        {
            Func<TokenContext, Task<string>> obtainToken = async (TokenContext tokenContext) =>
            {
                var jwtFromServer = await EmulateServerResponseToBuildTokenRequest(tokenContext, lifeTimeMin);

                return jwtFromServer;
            };

            return obtainToken;
        }


        public static Task<string> EmulateServerResponseToBuildTokenRequest(TokenContext tokenContext, double lifeTimeMin = 10)
        {
            var serverResponse = Task<string>.Factory.StartNew(() =>
            {
                Thread.Sleep(1000); // simulation of long-term processing
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
                var identity = SomeHash(tokenContext.Identity);
                return builder.GenerateToken(identity, data).ToString();
            }
            );

            return serverResponse;
        }

        private static string SomeHash(string identity)
        {
            return String.IsNullOrWhiteSpace(identity) ? "my_default_identity" : identity;
        }

        public static TokenContext PullTokenContext(){
            return new TokenContext(null, "get", false, "keyknox");
        }

        public static TokenContext PushTokenContext()
        {
            return new TokenContext(null, "put", false, "keyknox");
        }

        public static TokenContext PushTokenContext1()
        {
            return new TokenContext(null, "put", false, "keyknox");
        }
    }
}
