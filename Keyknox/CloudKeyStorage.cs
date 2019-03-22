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
        private Dictionary<string, CloudEntry> cloudEntries;
        private DecryptedKeyknoxValue previousDecryptedKeyknoxValue;
        private bool syncWasCalled;
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
            var decryptedKeyknoxValue = await keyknoxManager.PullValueAsync();
            syncWasCalled = true;
            this.previousDecryptedKeyknoxValue = decryptedKeyknoxValue;
            cloudEntries = serializer.Deserialize(decryptedKeyknoxValue.Value);

            return cloudEntries;
        }
        public async Task DeteleEntry(string name)
        {
            // todo error if !syncWasCalled
            if (!cloudEntries.ContainsKey(name))
            {
                throw new Exception("missing key");
            }
            cloudEntries.Remove(name);

            var entries = serializer.Serialize(cloudEntries);
            var decryptedKeyknoxVal = await keyknoxManager.PushValueAsync(Bytes.FromString(entries), previousDecryptedKeyknoxValue.KeyknoxHash);
        }

        public async Task DeteleAll()
        {
            // todo error if !syncWasCalled
            var decryptedKeyknoxVal = await keyknoxManager.ResetValueAsync();
            this.previousDecryptedKeyknoxValue = decryptedKeyknoxVal;

            cloudEntries = serializer.Deserialize(decryptedKeyknoxVal.Value);
        }

        public bool ExistsEntry(string name)
        {
            return this.cloudEntries.ContainsKey(name);
            // todo !sync -> error
            throw new NotImplementedException();
        }

        public List<CloudEntry> RetrieveAllEntries()
        {
            // todo !sync -> error
            return this.cloudEntries.Values.ToList();
            //throw new NotImplementedException();
        }

        public CloudEntry RetrieveEntry(string name)
        {
            // todo error if !syncWasCalled
            if (!cloudEntries.ContainsKey(name))
            {
                throw new Exception("missing key");
            }
            return cloudEntries[name];
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
            if (this.cloudEntries.Keys.Intersect(names).Any())
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
                this.cloudEntries.Add(cloudEntry.Name, cloudEntry);
            }

            var decryptedKeyknoxVal = await keyknoxManager.PushValueAsync(
                serializer.Serialize(this.cloudEntries), previousDecryptedKeyknoxValue.KeyknoxHash);
            this.previousDecryptedKeyknoxValue = decryptedKeyknoxVal;


            cloudEntries = serializer.Deserialize(decryptedKeyknoxVal.Value);
            return addedCloudEntries;
        }

        public async Task<CloudEntry> UpdateEntry(string name, byte[] data, Dictionary<string, string> meta = null)
        {
            // todo error if !syncWasCalled

            if (!cloudEntries.ContainsKey(name))
            {
                throw new NotImplementedException();
            }
            var cloudEntry = cloudEntries[name];
            cloudEntry.ModificationDate = DateTime.Now;
            cloudEntry.Data = data;
            cloudEntry.Meta = meta;
            this.cloudEntries.Add(name, cloudEntry);
            var decryptedKeyknoxVal = await keyknoxManager.PushValueAsync(
                serializer.Serialize(this.cloudEntries),
                previousDecryptedKeyknoxValue.KeyknoxHash);
            this.previousDecryptedKeyknoxValue = decryptedKeyknoxVal;

            cloudEntries = serializer.Deserialize(decryptedKeyknoxVal.Value);
            return cloudEntry;
        }

        public async Task<DecryptedKeyknoxValue> UpdateRecipients(IPublicKey[] publicKeys, IPrivateKey privateKey)
        {
            // todo error if !syncWasCalled
            if (previousDecryptedKeyknoxValue.Value == null || !previousDecryptedKeyknoxValue.Value.Any())
            {
                return previousDecryptedKeyknoxValue;
            }
            var decryptedKeyknoxValue = await this.keyknoxManager.UpdateRecipientsAndPushValue(
                previousDecryptedKeyknoxValue.Value, previousDecryptedKeyknoxValue.KeyknoxHash, publicKeys, privateKey);

            this.previousDecryptedKeyknoxValue = decryptedKeyknoxValue;

            cloudEntries = serializer.Deserialize(decryptedKeyknoxValue.Value);

            return previousDecryptedKeyknoxValue;
        }
    }
}
