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

    /// <summary>
    /// Cloud key storage stores keys in Virgil cloud using E2EE
    /// </summary>
    public class CloudKeyStorage : ICloudKeyStorage
    {
        private static readonly SemaphoreSlim SemaphoreSlim = new SemaphoreSlim(1, 1);

        private IKeyknoxManager keyknoxManager;
        private ICloudSerializer serializer;
        private CloudKeyCache cloudKeyCache;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Keyknox.CloudKeyStorage"/> class.
        /// </summary>
        /// <param name="keyknoxManager">Keyknox manager.</param>
        /// <param name="serializer">Serializer.</param>
        public CloudKeyStorage(
            IKeyknoxManager keyknoxManager,
            ICloudSerializer serializer = null)
        {
            this.keyknoxManager = keyknoxManager;
            this.serializer = serializer ?? new CloudSerializer(new NewtonsoftJsonExtendedSerializer());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Keyknox.CloudKeyStorage"/> class.
        /// </summary>
        /// <param name="accessTokenProvider">Access token provider for getting Access Token.</param>
        /// <param name="privateKey">Private key used for decryption and signing.</param>
        /// <param name="publicKeys">Public keys used for encryption and signature verification.</param>
        public CloudKeyStorage(
            IAccessTokenProvider accessTokenProvider,
            IPrivateKey privateKey,
            IPublicKey[] publicKeys)
            : this(new KeyknoxManager(accessTokenProvider, privateKey, publicKeys))
        {
        }

        /// <summary>
        /// Whether the storage was synchronized.
        /// </summary>
        /// <returns><c>true</c>, if storage was synchronized, <c>false</c> otherwise.</returns>
        public bool IsSynchronized()
        {
            return this.cloudKeyCache != null && this.cloudKeyCache.Response != null;
        }

        /// <summary>
        /// Uploads all cloud entries from the cloud.
        /// </summary>
        /// <returns>Uploaded cloud entries and their names.</returns>
        public async Task<Dictionary<string, CloudEntry>> UploadAllAsync()
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

        /// <summary>
        /// Deletes entry with the specified name from the cloud.
        /// </summary>
        /// <param name="name">Entry name.</param>
        public async Task DeteleAsync(string name)
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

        /// <summary>
        /// Deletes all entries from the cloud.
        /// </summary>
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

        /// <summary>
        /// Checks if entry with the specified name exists in the cloud.
        /// </summary>
        /// <returns>True if exists.</returns>
        /// <param name="name">Cloud entry name.</param>
        public bool Exists(string name)
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

        /// <summary>
        /// Retrieves all entries from the cloud.
        /// </summary>
        /// <returns>All cloud entries.</returns>
        public List<CloudEntry> RetrieveAll()
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

        /// <summary>
        /// Retrieves the cloud entry.
        /// </summary>
        /// <returns>The stored cloud entry.</returns>
        /// <param name="name">Cloud entry name.</param>
        public CloudEntry Retrieve(string name)
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

        /// <summary>
        /// Stores an entry with the specified name, data and meta in the cloud.
        /// </summary>
        /// <returns>The saved cloud entry.</returns>
        /// <param name="name">Entry name.</param>
        /// <param name="data">Data to be stored.</param>
        /// <param name="meta">Meta to be stored.</param>
        public async Task<CloudEntry> StoreAsync(string name, byte[] data, IDictionary<string, string> meta)
        {
            this.ThrowExceptionIfNotSynchronized();

            var entry = new KeyEntry() { Name = name, Value = data, Meta = meta };
            var stored = await this.StoreAsync(new List<KeyEntry> { entry });
            return stored.First();
        }

        /// <summary>
        /// Stores the specified key entries in the cloud.
        /// </summary>
        /// <returns>Saved cloud entries.</returns>
        /// <param name="keyEntries">Key entries to be stored.</param>
        public async Task<List<CloudEntry>> StoreAsync(List<KeyEntry> keyEntries)
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
                        Meta = keyEntry.Meta,
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

        /// <summary>
        /// Updates a cloud entry with the specified name.
        /// </summary>
        /// <returns>The updated cloud entry.</returns>
        /// <param name="name">Name of entry to be updated.</param>
        /// <param name="data">New data to be stored in the entry with the specified name.</param>
        public async Task<CloudEntry> UpdateAsync(string name, byte[] data, IDictionary<string, string> meta = null)
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

        /// <summary>
        /// Updates public keys for ecnryption and signature verification
        /// and private key for decryption and signature generation
        /// </summary>
        /// <param name="publicKeys">New public keys for ecnryption and signature verification.</param>
        /// <param name="privateKey">New private key for decryption and signature generation.</param>
        /// <returns>Encrypted and signed by specified keys <see cref="T:Keyknox.DecryptedKeyknoxValue"/></returns>
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
            if (!this.IsSynchronized())
            {
                throw new SyncException("The cloud storage isn't synchronized.");
            }
        }
    }
}
