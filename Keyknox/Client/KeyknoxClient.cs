using System;
using System.Net.Http;
using System.Threading.Tasks;
using Virgil.SDK.Common;

namespace Keyknox.Client
{
    public class KeyknoxClient : HttpClientBase, IKeyknoxClient 
    {
        public KeyknoxClient(IJsonSerializer serializer, string serviceUrl = null):
        base(serializer, serviceUrl){
        }

        public async Task<EncryptedKeyknoxValue> PullValueAsync(string token)
        {
            var response = await this.SendAsync(
                HttpMethod.Get, $"keyknox/v1", token).ConfigureAwait(false);

            return new EncryptedKeyknoxValue()
            {
                Value = Bytes.FromString(response.Value, StringEncoding.BASE64),
                Meta = Bytes.FromString(response.Meta, StringEncoding.BASE64),
                Version = response.Version,
                KeyknoxHash = Bytes.FromString(response.KeyknoxHash, StringEncoding.BASE64)
            };
        }

        public async Task<EncryptedKeyknoxValue> PushValueAsync(byte[] meta, byte[] value, byte[] previousHash, string token)
        {
            var model = new BodyModel()
            {
                Meta = Bytes.ToString(meta, StringEncoding.BASE64),
                Value = Bytes.ToString(value, StringEncoding.BASE64),
            };
            if (previousHash != null){
                model.KeyknoxHash = Bytes.ToString(previousHash, StringEncoding.BASE64);
            }

            var response = await this.SendAsync(
                HttpMethod.Put, $"keyknox/v1", token, model).ConfigureAwait(false);

            return new EncryptedKeyknoxValue()
            {
                Value = Bytes.FromString(response.Value, StringEncoding.BASE64),
                Meta = Bytes.FromString(response.Meta, StringEncoding.BASE64),
                Version = response.Version,
                KeyknoxHash = Bytes.FromString(response.KeyknoxHash, StringEncoding.BASE64)
            };
        }

        public async Task<DecryptedKeyknoxValue> ResetValueAsync(string token)
        {
            var response = await this.SendAsync(
                HttpMethod.Put, $"keyknox/v1/reset", token).ConfigureAwait(false);

            return new DecryptedKeyknoxValue()
            {
                Value = Bytes.FromString(response.Value, StringEncoding.BASE64),
                Meta = Bytes.FromString(response.Meta, StringEncoding.BASE64),
                Version = response.Version,
                KeyknoxHash = Bytes.FromString(response.KeyknoxHash, StringEncoding.BASE64)
            };
        }
    }
}
