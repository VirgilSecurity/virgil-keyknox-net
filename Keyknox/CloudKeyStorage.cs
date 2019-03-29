/*
 * Copyright (C) 2015-2019 Virgil Security Inc.
 *
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are
 * met:
 *
 *     (1) Redistributions of source code must retain the above copyright
 *     notice, this list of conditions and the following disclaimer.
 *
 *     (2) Redistributions in binary form must reproduce the above copyright
 *     notice, this list of conditions and the following disclaimer in
 *     the documentation and/or other materials provided with the
 *     distribution.
 *
 *     (3) Neither the name of the copyright holder nor the names of its
 *     contributors may be used to endorse or promote products derived from
 *     this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE AUTHOR ''AS IS'' AND ANY EXPRESS OR
 * IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY DIRECT,
 * INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
 * SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
 * HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
 * STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING
 * IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
 * POSSIBILITY OF SUCH DAMAGE.
 *
 * Lead Maintainer: Virgil Security Inc. <support@virgilsecurity.com>
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Keyknox.Utils;
using Virgil.CryptoAPI;
using Virgil.SDK;
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
        public async Task<Dictionary<string, CloudEntry>> RetrieveCloudEntries()
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
        public async Task DeteleEntryAsync(string name)
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

        public async Task DeteleAllAsync()
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
            var entry = new KeyEntry() { Name = name, Value = data, Meta = meta };
            var stored = await StoreEntries(new List<KeyEntry> { entry });
            return stored.First();
        }

        public async Task<List<CloudEntry>> StoreEntries(List<KeyEntry> keyEntries)
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
                        Data = keyEntry.Value,
                        CreationDate = DateTime.UtcNow,
                        Meta = (Dictionary<string, string>)keyEntry.Meta,
                        ModificationDate = DateTime.UtcNow
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

        public async Task<CloudEntry> UpdateEntryAsync(string name, byte[] data, Dictionary<string, string> meta = null)
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
