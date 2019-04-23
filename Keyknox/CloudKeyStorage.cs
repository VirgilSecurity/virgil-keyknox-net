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

namespace Keyknox
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Keyknox.CloudKeyStorageException;
    using Keyknox.Utils;
    using Virgil.CryptoAPI;
    using Virgil.SDK;
    using Virgil.SDK.Common;
    using Virgil.SDK.Web.Authorization;

    public class CloudKeyStorage
    {
        private static readonly SemaphoreSlim SemaphoreSlim = new SemaphoreSlim(1, 1);

        private KeyknoxManager keyknoxManager;
        private ICloudSerializer serializer;
        private CloudKeyCache cloudKeyCache;

        public CloudKeyStorage(
            KeyknoxManager keyknoxManager,
            ICloudSerializer serializer = null)
        {
            this.keyknoxManager = keyknoxManager;
            this.serializer = serializer ?? new CloudSerializer(new NewtonsoftJsonExtendedSerializer());
        }

        public CloudKeyStorage(
            IAccessTokenProvider accessTokenProvider,
            IPrivateKey privateKey,
            IPublicKey[] publicKeys)
            : this(new KeyknoxManager(accessTokenProvider, privateKey, publicKeys))
        {
        }

        public bool IsStorageSynchronized()
        {
            return this.cloudKeyCache != null && this.cloudKeyCache.Response != null;
        }

        public async Task<Dictionary<string, CloudEntry>> RetrieveCloudEntriesAsync()
        {
            await SemaphoreSlim.WaitAsync();
            try
            {
                var decryptedKeyknoxValue = await this.keyknoxManager.PullValueAsync();
                this.cloudKeyCache = new CloudKeyCache(decryptedKeyknoxValue, this.serializer);
            }
            finally
            {
                SemaphoreSlim.Release();
            }

            return this.cloudKeyCache.Entries;
        }

        public async Task DeteleEntryAsync(string name)
        {
            this.ThrowExceptionIfNotSynchronized();

            await SemaphoreSlim.WaitAsync();
            try
            {
                if (!this.cloudKeyCache.Entries.ContainsKey(name))
                {
                    throw new MissingEntryException("missing key");
                }

                this.cloudKeyCache.Entries.Remove(name);

                var decryptedKeyknoxVal = await this.keyknoxManager.PushValueAsync(
                    this.serializer.Serialize(this.cloudKeyCache.Entries),
                    this.cloudKeyCache.Response.KeyknoxHash);
                this.cloudKeyCache.Refresh(decryptedKeyknoxVal);
            }
            finally
            {
                SemaphoreSlim.Release();
            }
        }

        public async Task DeteleAllAsync()
        {
            await SemaphoreSlim.WaitAsync();
            try
            {
                var decryptedKeyknoxVal = await this.keyknoxManager.ResetValueAsync();
                if (this.cloudKeyCache != null)
                {
                    this.cloudKeyCache.Refresh(decryptedKeyknoxVal);
                }
                else
                {
                    this.cloudKeyCache = new CloudKeyCache(decryptedKeyknoxVal, this.serializer);
                }
            }
            finally
            {
                SemaphoreSlim.Release();
            }
        }

        public bool ExistsEntry(string name)
        {
            this.ThrowExceptionIfNotSynchronized();

            SemaphoreSlim.Wait();
            try
            {
                return this.cloudKeyCache.Entries.ContainsKey(name);
            }
            finally
            {
                SemaphoreSlim.Release();
            }
        }

        public List<CloudEntry> RetrieveAllEntries()
        {
            this.ThrowExceptionIfNotSynchronized();

            SemaphoreSlim.Wait();
            try
            {
                return this.cloudKeyCache.Entries.Values.ToList();
            }
            finally
            {
                SemaphoreSlim.Release();
            }
        }

        public CloudEntry RetrieveEntry(string name)
        {
            this.ThrowExceptionIfNotSynchronized();

            SemaphoreSlim.Wait();
            try
            {
                if (!this.cloudKeyCache.Entries.ContainsKey(name))
                {
                     throw new MissingEntryException($"Can't find an entry with the name: {name}.");
                }

                return this.cloudKeyCache.Entries[name];
            }
            finally
            {
                SemaphoreSlim.Release();
            }
        }

        public async Task<CloudEntry> StoreAsync(string name, byte[] data, Dictionary<string, string> meta)
        {
            this.ThrowExceptionIfNotSynchronized();

            var entry = new KeyEntry() { Name = name, Value = data, Meta = meta };
            var stored = await this.StoreEntriesAsync(new List<KeyEntry> { entry });
            return stored.First();
        }

        public async Task<List<CloudEntry>> StoreEntriesAsync(List<KeyEntry> keyEntries)
        {
            this.ThrowExceptionIfNotSynchronized();

            var names = keyEntries.Select(keyEntry => keyEntry.Name);

            SemaphoreSlim.Wait();
            try
            {
                if (this.cloudKeyCache.Entries.Keys.Intersect(names).Any())
                {
                    throw new NonUniqueEntryException("An entry with the same name already exists.");
                }

                var addedCloudEntries = new List<CloudEntry>();
                foreach (var keyEntry in keyEntries)
                {
                    var cloudEntry = new CloudEntry()
                    {
                        Name = keyEntry.Name,
                        Data = keyEntry.Value,
                        CreationDate = DateTime.UtcNow.RoundTicks(),
                        Meta = (Dictionary<string, string>)keyEntry.Meta,
                        ModificationDate = DateTime.UtcNow.RoundTicks()
                    };
                    addedCloudEntries.Add(cloudEntry);
                    this.cloudKeyCache.Entries.Add(cloudEntry.Name, cloudEntry);
                }

                var decryptedKeyknoxVal = await this.keyknoxManager.PushValueAsync(
                    this.serializer.Serialize(this.cloudKeyCache.Entries),
                    this.cloudKeyCache.Response.KeyknoxHash);

                this.cloudKeyCache.Refresh(decryptedKeyknoxVal);
                return addedCloudEntries;
            }
            finally
            {
                SemaphoreSlim.Release();
            }
        }

        public async Task<CloudEntry> UpdateEntryAsync(string name, byte[] data, Dictionary<string, string> meta = null)
        {
            this.ThrowExceptionIfNotSynchronized();

            SemaphoreSlim.Wait();
            try
            {
                if (!this.cloudKeyCache.Entries.ContainsKey(name))
                {
                    throw new MissingEntryException($"Can't find an entry with the name: {name}.");
                }

                var cloudEntry = this.cloudKeyCache.Entries[name];
                cloudEntry.ModificationDate = DateTime.Now.RoundTicks();
                cloudEntry.Data = data;
                cloudEntry.Meta = meta;
                var decryptedKeyknoxVal = await this.keyknoxManager.PushValueAsync(
                    this.serializer.Serialize(this.cloudKeyCache.Entries),
                    this.cloudKeyCache.Response.KeyknoxHash);
                this.cloudKeyCache.Refresh(decryptedKeyknoxVal);

                return cloudEntry;
            }
            finally
            {
                SemaphoreSlim.Release();
            }
        }

        public async Task<DecryptedKeyknoxValue> UpdateRecipientsAsync(IPublicKey[] publicKeys, IPrivateKey privateKey)
        {
            this.ThrowExceptionIfNotSynchronized();

            SemaphoreSlim.Wait();
            try
            {
                if (this.cloudKeyCache.Response.Value == null || !this.cloudKeyCache.Response.Value.Any())
                {
                    return this.cloudKeyCache.Response;
                }

                var decryptedKeyknoxValue = await this.keyknoxManager.UpdateRecipientsAsync(
                    this.cloudKeyCache.Response.Value, this.cloudKeyCache.Response.KeyknoxHash, publicKeys, privateKey);

                this.cloudKeyCache.Refresh(decryptedKeyknoxValue);
                return this.cloudKeyCache.Response;
            }
            finally
            {
                SemaphoreSlim.Release();
            }
        }

        private void ThrowExceptionIfNotSynchronized()
        {
            if (!this.IsStorageSynchronized())
            {
                throw new SyncException("The cloud storage isn't synchronized.");
            }
        }
    }
}
