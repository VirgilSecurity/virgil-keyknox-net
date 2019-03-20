using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Keyknox.Utils;
using Virgil.CryptoAPI;
using Virgil.SDK.Common;
using Virgil.SDK.Web.Authorization;

namespace Keyknox
{
    public class CloudKeyStorage : IKeyStorage
    {
        private KeyknoxManager keyknoxManager;
        private Utils.IJsonSerializer serializer;
       // private Lazy<Task<Dictionary<string, CloudEntry>>> cloudEntries;
        private Dictionary<string, CloudEntry> cloudEntries;
        private byte[] keyknoxHash;
        private bool syncWasCalled;
        public CloudKeyStorage(KeyknoxManager keyknoxManager, Utils.IJsonSerializer serializer = null)
        {
            this.keyknoxManager = keyknoxManager;
            this.serializer = serializer ?? new Utils.NewtonsoftJsonSerializer();
           // this.cloudEntries = new Lazy<Task<Dictionary<string, CloudEntry>>>(
            //    () => RetrieveCloudEntries()
            //);

        }

        public CloudKeyStorage(IAccessTokenProvider accessTokenProvider,
                              IPrivateKey privateKey,
                               IPublicKey[] publicKeys) : this(new KeyknoxManager(accessTokenProvider, privateKey, publicKeys))
        {
        }
        private async Task<Dictionary<string, CloudEntry>> RetrieveCloudEntries(){
            var decryptedKeyknoxValue = await keyknoxManager.PullValueAsync();
            var entries = serializer.Deserialize<List<CloudEntry>>(Bytes.ToString(decryptedKeyknoxValue.Value));
            var namedEntries = new Dictionary<string, CloudEntry>();
            syncWasCalled = true;
            this.keyknoxHash = decryptedKeyknoxValue.KeyknoxHash;
            entries.ForEach(entry => namedEntries.Add(entry.Name, entry));
            cloudEntries = namedEntries;
            return namedEntries;
           // cacheEntries(deserializeEntries(response.value), true)
           // this.decryptedKeyknoxData = response
        }

    }
}
