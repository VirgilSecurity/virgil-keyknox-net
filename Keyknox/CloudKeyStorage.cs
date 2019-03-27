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
    public class CloudKeyStorage
    {
        private KeyknoxManager keyknoxManager;
        private ICloudSerializer serializer;
        // private Lazy<Task<Dictionary<string, CloudEntry>>> cloudEntries;
        //private Dictionary<string, CloudEntry> cloudEntries;
        private CloudKeyCache cloudKeyCache;
        //private DecryptedKeyknoxValue previousDecryptedKeyknoxValue;
        private bool syncWasCalled;
        static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);

        public CloudKeyStorage(KeyknoxManager keyknoxManager, ICloudSerializer serializer = null)
        {
            this.keyknoxManager = keyknoxManager;
            this.serializer = serializer ?? new CloudSerializer(new NewtonsoftJsonSerializer());
            // this.cloudEntries = new Lazy<Task<Dictionary<string, CloudEntry>>>(
            //    () => RetrieveCloudEntries()
            //);
        }

        public CloudKeyStorage(IAccessTokenProvider accessTokenProvider,
                              IPrivateKey privateKey,
                               IPublicKey[] publicKeys) : this(new KeyknoxManager(accessTokenProvider, privateKey, publicKeys))
        {
        }
        private async Task<Dictionary<string, CloudEntry>> RetrieveCloudEntries()
        {
            await semaphoreSlim.WaitAsync();
            try{
                var decryptedKeyknoxValue = await keyknoxManager.PullValueAsync();
                syncWasCalled = true;
                cloudKeyCache = new CloudKeyCache(decryptedKeyknoxValue, serializer);
                //this.previousDecryptedKeyknoxValue = decryptedKeyknoxValue;
                //cloudEntries = serializer.Deserialize(decryptedKeyknoxValue.Value);
            }finally{
                semaphoreSlim.Release();
            }

            return cloudKeyCache.Entries;
        }
        public async Task DeteleEntry(string name)
        {
            // todo error if !syncWasCalled
            await semaphoreSlim.WaitAsync();
            try{
                if (!cloudKeyCache.Entries.ContainsKey(name))
                {
                    throw new Exception("missing key");
                }
                cloudKeyCache.Entries.Remove(name);

                var decryptedKeyknoxVal = await keyknoxManager.PushValueAsync(
                    serializer.Serialize(cloudKeyCache.Entries),
                    cloudKeyCache.Response.KeyknoxHash);
                cloudKeyCache.Refresh(decryptedKeyknoxVal);
                //this.previousDecryptedKeyknoxValue = decryptedKeyknoxVal;
               // cloudEntries = serializer.Deserialize(decryptedKeyknoxVal.Value);
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        public async Task DeteleAll()
        {
            // todo error if !syncWasCalled
            await semaphoreSlim.WaitAsync();
            try
            {
                var decryptedKeyknoxVal = await keyknoxManager.ResetValueAsync();
                cloudKeyCache.Refresh(decryptedKeyknoxVal);
              //  this.previousDecryptedKeyknoxValue = decryptedKeyknoxVal;

              //  cloudEntries = serializer.Deserialize(decryptedKeyknoxVal.Value);
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        public bool ExistsEntry(string name)
        {
            // todo !sync -> error

            semaphoreSlim.Wait();
            try
            {
                return this.cloudKeyCache.Entries.ContainsKey(name);
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        public List<CloudEntry> RetrieveAllEntries()
        {
            // todo !sync -> error
            semaphoreSlim.Wait();
            try
            {
                return this.cloudKeyCache.Entries.Values.ToList();
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        public CloudEntry RetrieveEntry(string name)
        {
            // todo error if !syncWasCalled
            semaphoreSlim.Wait();
            try
            {
                if (!this.cloudKeyCache.Entries.ContainsKey(name))
                {
                    throw new Exception("missing key");
                }
                return this.cloudKeyCache.Entries[name];
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        public async Task<CloudEntry> Store(string name, byte[] data, Dictionary<string, string> meta)
        {
            // todo error if !syncWasCalled
            var entry = new KeyEntry() { Name = name, Data = data, Meta = meta };
            var stored = await StoreEntries(new[] { entry });
            return stored.First();
        }

        public async Task<List<CloudEntry>> StoreEntries(KeyEntry[] keyEntries)
        {
            // todo error if !syncWasCalled
            var names = keyEntries.Select(keyEntry => keyEntry.Name);

            semaphoreSlim.Wait();
            try
            {
                if (this.cloudKeyCache.Entries.Keys.Intersect(names).Any())
                {
                    throw new NotImplementedException();
                }
                var addedCloudEntries = new List<CloudEntry>();
                foreach (var keyEntry in keyEntries)
                {
                    var cloudEntry = new CloudEntry()
                    {
                        Name = keyEntry.Name,
                        Data = keyEntry.Data,
                        CreationDate = DateTime.Now,
                        Meta = keyEntry.Meta,
                        ModificationDate = DateTime.Now
                    };
                    addedCloudEntries.Add(cloudEntry);
                    this.cloudKeyCache.Entries.Add(cloudEntry.Name, cloudEntry);
                }

                var decryptedKeyknoxVal = await keyknoxManager.PushValueAsync(
                    serializer.Serialize(this.cloudKeyCache.Entries), this.cloudKeyCache.Response.KeyknoxHash);

                cloudKeyCache.Refresh(decryptedKeyknoxVal);
               // this.previousDecryptedKeyknoxValue = decryptedKeyknoxVal;


              //  cloudEntries = serializer.Deserialize(decryptedKeyknoxVal.Value);
                return addedCloudEntries;
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        public async Task<CloudEntry> UpdateEntry(string name, byte[] data, Dictionary<string, string> meta = null)
        {
            // todo error if !syncWasCalled
            semaphoreSlim.Wait();
            try
            {
                if (!this.cloudKeyCache.Entries.ContainsKey(name))
                {
                    throw new NotImplementedException();
                }
                var cloudEntry = this.cloudKeyCache.Entries[name];
                cloudEntry.ModificationDate = DateTime.Now;
                cloudEntry.Data = data;
                cloudEntry.Meta = meta;
                this.cloudKeyCache.Entries.Add(name, cloudEntry);
                var decryptedKeyknoxVal = await keyknoxManager.PushValueAsync(
                    serializer.Serialize(this.cloudKeyCache.Entries),
                    this.cloudKeyCache.Response.KeyknoxHash);
                cloudKeyCache.Refresh(decryptedKeyknoxVal);
               // this.previousDecryptedKeyknoxValue = decryptedKeyknoxVal;

                //cloudEntries = serializer.Deserialize(decryptedKeyknoxVal.Value);
                return cloudEntry;
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        public async Task<DecryptedKeyknoxValue> UpdateRecipients(IPublicKey[] publicKeys, IPrivateKey privateKey)
        {
            // todo error if !syncWasCalled
            semaphoreSlim.Wait();
            try
            {
                if (this.cloudKeyCache.Response.Value == null || !this.cloudKeyCache.Response.Value.Any())
                {
                    return this.cloudKeyCache.Response;
                }
                var decryptedKeyknoxValue = await this.keyknoxManager.UpdateRecipientsAndPushValue(
                    this.cloudKeyCache.Response.Value, this.cloudKeyCache.Response.KeyknoxHash, publicKeys, privateKey);

                cloudKeyCache.Refresh(decryptedKeyknoxValue);
             //   this.previousDecryptedKeyknoxValue = decryptedKeyknoxValue;

              //  cloudEntries = serializer.Deserialize(decryptedKeyknoxValue.Value);

                return cloudKeyCache.Response;
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }
    }
}
