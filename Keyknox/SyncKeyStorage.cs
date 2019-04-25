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

    public class SyncKeyStorage
    {
        public readonly string Identity;
        private CloudKeyStorage cloudStorage;
        private LocalKeyStorage localStorage;
        private bool isStorageSynchronized;

        public SyncKeyStorage(
            string identity,
            CloudKeyStorage cloudKeyStorage,
            LocalKeyStorage localStorage)
        {
            this.Identity = identity;
            this.cloudStorage = cloudKeyStorage;
            this.localStorage = localStorage;
        }

#if OSX
        public SyncKeyStorage(
            string identity,
            IAccessTokenProvider accessTokenProvider,
            IPrivateKey privateKey,
            IPublicKey[] publicKeys)
        {
            this.Identity = identity;
            this.cloudStorage = new CloudKeyStorage(accessTokenProvider, privateKey, publicKeys);
            this.localStorage = new LocalKeyStorage(identity);
        }
#else
        public SyncKeyStorage(
            string identity,
            string password,
            IAccessTokenProvider accessTokenProvider,
            IPrivateKey privateKey,
            IPublicKey[] publicKeys)
        {
            this.identity = identity;
            this.cloudStorage = new CloudKeyStorage(accessTokenProvider, privateKey, publicKeys);
            this.localStorage = new LocalKeyStorage(password, identity);
        }
#endif

        public async Task SynchronizeAsync()
        {
            var localEntriesNames = this.localStorage.Names();

            var cloudEntries = await this.cloudStorage.RetrieveCloudEntriesAsync();
            var cloudEntriesNames = cloudEntries.Keys.ToList();

            var entriesToCompare = cloudEntriesNames.Intersect(localEntriesNames);
            await this.CompareAndUpdateEntries(entriesToCompare, cloudEntries);

            var cloudEntriesToStore = cloudEntriesNames.Except(localEntriesNames);
            this.StoreCloudEntries(cloudEntriesToStore, cloudEntries);

            var localEntriesToDelete = localEntriesNames.Except(cloudEntriesNames);
            this.DeleteLocalEntries(localEntriesToDelete);
            this.isStorageSynchronized = true;
        }

        public KeyEntry RetrieveEntry(string name)
        {
            this.ThrowExceptionIfNotSynchronized();
            return this.localStorage.Load(name);
        }

        public async Task DeleteEntriesAsync(List<string> names)
        {
            this.ThrowExceptionIfNotSynchronized();
            var localEntriesNames = new List<string>(this.localStorage.Names());
            var missingEntryNames = names.Except(localEntriesNames);
            if (missingEntryNames.Any())
            {
                throw new MissingEntryException($"Entries are missing: {string.Join(", ", missingEntryNames)}");
            }

            foreach (var name in names)
            {
                await this.cloudStorage.DeteleEntryAsync(name);
                this.localStorage.Delete(name);
            }
        }

        public bool ExistsEntry(string name)
        {
            this.ThrowExceptionIfNotSynchronized();
            return this.localStorage.Exists(name);
        }

        public List<KeyEntry> RetrieveAllEntries()
        {
            this.ThrowExceptionIfNotSynchronized();
            var localEntriesNames = this.localStorage.Names();
            var keyEntries = new List<KeyEntry>();
            foreach (var name in localEntriesNames)
            {
                keyEntries.Add(this.localStorage.Load(name));
            }

            return keyEntries;
        }

        public async Task DeleteEntryAsync(string name)
        {
            this.ThrowExceptionIfNotSynchronized();
            await this.DeleteEntriesAsync(new List<string>() { name });
        }

        public async Task DeleteAllEntries()
        {
            this.ThrowExceptionIfNotSynchronized();
            await this.cloudStorage.DeteleAllAsync();
            this.DeleteLocalEntries(this.localStorage.Names());
        }

        public async Task<KeyEntry> StoreEntryAsync(string name, byte[] data, Dictionary<string, string> meta)
        {
            this.ThrowExceptionIfNotSynchronized();
            var keyEntry = new KeyEntry() { Name = name, Meta = meta, Value = data };
            var storedEntries = await this.StoreEntries(new List<KeyEntry>() { keyEntry });
            return storedEntries.First();
        }

        public async Task<List<KeyEntry>> StoreEntries(List<KeyEntry> entries)
        {
            this.ThrowExceptionIfNotSynchronized();
            var localEntriesNames = new List<string>(this.localStorage.Names());
            foreach (var entry in entries)
            {
                if (localEntriesNames.Contains(entry.Name) || this.cloudStorage.ExistsEntry(entry.Name))
                {
                    throw new NonUniqueEntryException($"Entry already exists: #{entry.Name}");
                }
            }

            var keyEntries = new List<KeyEntry>();
            var cloudEntries = await this.cloudStorage.StoreEntriesAsync(entries);

            foreach (var entry in cloudEntries)
            {
                KeyEntry keyEntry = this.CloneFromCloudToLocal(entry);
                keyEntries.Add(keyEntry);
            }

            return keyEntries;
        }

        private KeyEntry CloneFromCloudToLocal(CloudEntry entry)
        {
            var meta = new Dictionary<string, string>(entry.Meta);

            var keyEntry = new KeyEntry()
            {
                Name = entry.Name,
                Value = entry.Data,
                Meta = MetaDate.CopyAndAppendDatesTo(
                    entry.Meta,
                    entry.CreationDate,
                    entry.ModificationDate)
            };
            this.localStorage.Store(keyEntry);
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
                this.localStorage.Store(
                    new KeyEntry
                    {
                        Value = cloudEntries[name].Data,
                        Name = cloudEntries[name].Name,
                        Meta = cloudEntries[name].Meta
                    });
            }
        }

        private async Task CompareAndUpdateEntries(IEnumerable<string> names, Dictionary<string, CloudEntry> cloudEntries)
        {
            foreach (var name in names)
            {
                var localKeyEntry = this.localStorage.Load(name);
                var cloudEntry = cloudEntries[name];
                DateTime modificationDate = MetaDate.ExtractModificationDateFrom(
                    (Dictionary<string, string>)localKeyEntry.Meta);

                if (modificationDate < cloudEntry.ModificationDate)
                {
                    this.localStorage.Delete(name);
                    this.CloneFromCloudToLocal(cloudEntry);
                }

                if (modificationDate > cloudEntry.ModificationDate)
                {
                    await this.cloudStorage.UpdateEntryAsync(
                        name,
                        localKeyEntry.Value,
                        (Dictionary<string, string>)localKeyEntry.Meta);
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
