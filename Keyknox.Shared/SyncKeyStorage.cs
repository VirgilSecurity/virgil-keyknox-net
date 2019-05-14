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
    using System.Threading.Tasks;
    using Keyknox.CloudKeyStorageException;
    using Virgil.CryptoAPI;
    using Virgil.SDK;
    using Virgil.SDK.Web.Authorization;

    /// <summary>
    /// Synchronizes data between cloud and local storage.
    /// </summary>
    public class SyncKeyStorage
    {
        private ICloudKeyStorage cloudStorage;
        private ILocalKeyStorage localStorage;
        private bool isStorageSynchronized;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Keyknox.SyncKeyStorage"/> class.
        /// </summary>
        /// <param name="identity">User's identity to group keys in local storage.</param>
        /// <param name="cloudKeyStorage">A cloud key storage.</param>
        /// <param name="localStorage">A local storage.</param>
        public SyncKeyStorage(
            string identity,
            ICloudKeyStorage cloudKeyStorage,
            ILocalKeyStorage localStorage)
        {
            this.Identity = identity ?? throw new ArgumentNullException(nameof(identity));
            this.cloudStorage = cloudKeyStorage ?? throw new ArgumentNullException(nameof(cloudKeyStorage));
            this.localStorage = localStorage ?? throw new ArgumentNullException(nameof(localStorage));
        }

        /// <summary>
        /// User's identity to group by in local storage.
        /// </summary>
        /// <value>The identity.</value>
        public string Identity { get; private set; }

        /// <summary>
        /// Synchronizes data between Keyknox Cloud and local storage.
        /// </summary>
        /// <returns>The async.</returns>
        public async Task SynchronizeAsync()
        {
            var localEntriesNames = this.localStorage.Names();

            var cloudEntries = await this.cloudStorage.UploadAllAsync();
            var cloudEntriesNames = cloudEntries.Keys.ToList();

            var entriesToCompare = cloudEntriesNames.Intersect(localEntriesNames);
            await this.CompareAndUpdateEntries(entriesToCompare, cloudEntries);

            var cloudEntriesToStore = cloudEntriesNames.Except(localEntriesNames);
            this.StoreCloudEntries(cloudEntriesToStore, cloudEntries);

            var localEntriesToDelete = localEntriesNames.Except(cloudEntriesNames);
            this.DeleteLocalEntries(localEntriesToDelete);
            this.isStorageSynchronized = true;
        }

        /// <summary>
        /// Retrieves the synchronized entry.
        /// </summary>
        /// <returns>The stored key entry.</returns>
        /// <param name="name">Key entry name.</param>
        public KeyEntry Retrieve(string name)
        {
            this.ThrowExceptionIfNotSynchronized();
            return this.localStorage.Load(name ?? throw new ArgumentNullException(nameof(name)));
        }

        /// <summary>
        /// Deletes the synchronized entries from the cloud and the local storage.
        /// </summary>
        /// <param name="names">Names.</param>
        public async Task DeleteAsync(List<string> names)
        {
            this.ThrowExceptionIfNotSynchronized();
            if (!names.Any())
            {
                throw new ArgumentException($"empty {nameof(names)}.");
            }

            var localEntriesNames = new List<string>(this.localStorage.Names());
            var missingEntryNames = names.Except(localEntriesNames);
            if (missingEntryNames.Any())
            {
                throw new MissingEntryException($"Entries are missing: {string.Join(", ", missingEntryNames)}");
            }

            foreach (var name in names)
            {
                await this.cloudStorage.DeteleAsync(name);
                this.localStorage.Delete(name);
            }
        }

        /// <summary>
        /// Checks if entry with the specified name exists in synchronized local storage.
        /// </summary>
        /// <returns>True if exists.</returns>
        /// <param name="name">Entry name.</param>
        public bool Exists(string name)
        {
            this.ThrowExceptionIfNotSynchronized();
            return this.localStorage.Exists(name ?? throw new ArgumentNullException(nameof(name)));
        }

        /// <summary>
        /// Retrieves all entries from synchronized local storage.
        /// </summary>
        /// <returns>The all entries.</returns>
        public List<KeyEntry> RetrieveAll()
        {
            this.ThrowExceptionIfNotSynchronized();
            var localEntriesNames = this.localStorage.Names();
            var keyEntries = new List<KeyEntry>();
            foreach (var name in localEntriesNames)
            {
                var localEntry = this.localStorage.Load(name);
                keyEntries.Add(this.localStorage.Load(name));
            }

            return keyEntries;
        }

        /// <summary>
        /// Deletes entry with the specified name from the cloud and the local storage.
        /// </summary>
        /// <param name="name">Entry name.</param>
        public async Task DeleteAsync(string name)
        {
            this.ThrowExceptionIfNotSynchronized();
            await this.DeleteAsync(new List<string>() { name ?? throw new ArgumentNullException(nameof(name)) });
        }

        /// <summary>
        /// Deletes all from the cloud and the local storage.
        /// </summary>
        public async Task DeleteAllAsync()
        {
            this.ThrowExceptionIfNotSynchronized();
            await this.cloudStorage.DeteleAllAsync();
            this.DeleteLocalEntries(this.localStorage.Names());
        }

        /// <summary>
        /// Stores an entry with the specified name, data and meta.
        /// </summary>
        /// <returns>The saved key entry.</returns>
        /// <param name="name">Entry name.</param>
        /// <param name="data">Data to be stored.</param>
        /// <param name="meta">Meta to be stored.</param>
        public async Task<KeyEntry> StoreAsync(string name, byte[] data, IDictionary<string, string> meta)
        {
            this.ThrowExceptionIfNotSynchronized();
            var storedEntries = await this.StoreAsync(
                new List<KeyEntry>()
                {
                    new KeyEntry()
                    {
                        Name = name ?? throw new ArgumentNullException(nameof(name)),
                        Meta = meta,
                        Value = data ?? throw new ArgumentNullException(nameof(data))
                    }
                });
            return storedEntries.First();
        }

        /// <summary>
        /// Updates a key entry with the specified name.
        /// </summary>
        /// <returns>The updated key entry.</returns>
        /// <param name="name">Name of entry to be updated.</param>
        /// <param name="data">New data to be stored in the entry with the specified name.</param>
        /// <param name="meta">New meta to be stored in the entry with the specified name.</param>
        public async Task<KeyEntry> UpdateAsync(string name, byte[] data, IDictionary<string, string> meta)
        {
            this.ThrowExceptionIfNotSynchronized();
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (!this.localStorage.Exists(name) || !this.cloudStorage.Exists(name))
            {
                throw new MissingEntryException($"Entry is missing: {name}");
            }

            await this.cloudStorage.UpdateAsync(name, data, meta);
            this.localStorage.Delete(name);
            return this.CloneFromCloudToLocal(await this.cloudStorage.UpdateAsync(name, data, meta));
        }

        /// <summary>
        /// Stores the specified entries.
        /// </summary>
        /// <returns>Saved entries.</returns>
        /// <param name="entries">Entries to be stored.</param>
        public async Task<List<KeyEntry>> StoreAsync(List<KeyEntry> entries)
        {
            if (entries == null || !entries.Any())
            {
                throw new ArgumentException($"empty {nameof(entries)}.");
            }

            this.ThrowExceptionIfNotSynchronized();
            var localEntriesNames = new List<string>(this.localStorage.Names());
            foreach (var entry in entries)
            {
                if (localEntriesNames.Contains(entry.Name) || this.cloudStorage.Exists(entry.Name))
                {
                    throw new NonUniqueEntryException($"Entry already exists: #{entry.Name}");
                }
            }

            var keyEntries = new List<KeyEntry>();
            var cloudEntries = await this.cloudStorage.StoreAsync(entries);

            foreach (var entry in cloudEntries)
            {
                KeyEntry keyEntry = this.CloneFromCloudToLocal(entry);
                keyEntries.Add(keyEntry);
            }

            return keyEntries;
        }

        /// <summary>
        /// Updates public keys for ecnryption and signature verification
        /// and private key for decryption and signature generation
        /// </summary>
        /// <param name="publicKeys">New public keys for ecnryption and signature verification.</param>
        /// <param name="privateKey">New private key for decryption and signature generation.</param>
        public async Task UpdateRecipientsAsync(IPublicKey[] publicKeys, IPrivateKey privateKey)
        {
            await this.cloudStorage.UpdateRecipientsAsync(publicKeys, privateKey);
        }

        private KeyEntry CloneFromCloudToLocal(CloudEntry entry)
        {
            var keyEntry = new KeyEntry()
            {
                Name = entry.Name,
                Value = entry.Data,
                Meta = entry.Meta
            };
            this.localStorage.Store(keyEntry, entry.CreationDate, entry.ModificationDate);
            return keyEntry;
        }

        private void DeleteLocalEntries(IEnumerable<string> names)
        {
            foreach (var name in names)
            {
                this.localStorage.Delete(name);
            }
        }

        private void StoreCloudEntries(IEnumerable<string> names, Dictionary<string, CloudEntry> cloudEntries)
        {
            foreach (var name in names)
            {
                this.CloneFromCloudToLocal(cloudEntries[name]);
            }
        }

        private async Task CompareAndUpdateEntries(IEnumerable<string> names, Dictionary<string, CloudEntry> cloudEntries)
        {
            foreach (var name in names)
            {
                var localKeyEntry = this.localStorage.LoadFull(name);
                var cloudEntry = cloudEntries[name];
                DateTime modificationDate = MetaDate.ExtractModificationDateFrom(
                    localKeyEntry.Meta);

                if (modificationDate < cloudEntry.ModificationDate)
                {
                    this.localStorage.Delete(name);
                    this.CloneFromCloudToLocal(cloudEntry);
                }

                if (modificationDate > cloudEntry.ModificationDate)
                {
                    await this.cloudStorage.UpdateAsync(
                        name,
                        localKeyEntry.Value,
                        localKeyEntry.Meta);
                    this.localStorage.Delete(name);
                    this.CloneFromCloudToLocal(cloudEntry);
                }
            }
        }

        private void ThrowExceptionIfNotSynchronized()
        {
            if (!this.isStorageSynchronized)
            {
                throw new SyncException("The cloud storage isn't synchronized.");
            }
        }
    }
}
