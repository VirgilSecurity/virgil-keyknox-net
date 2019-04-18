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
    using Virgil.SDK;

    public class SyncKeyStorage
    {
        private string identity;
        private CloudKeyStorage cloudKeyStorage;
        private KeyStorage localStorage;
        private bool IsStorageSynchronized;

        public SyncKeyStorage(
            string identity,
            CloudKeyStorage cloudKeyStorage,
            KeyStorage localStorage)
        {
            this.identity = identity;
            this.cloudKeyStorage = cloudKeyStorage;
            this.localStorage = localStorage;
        }

        public async Task Synchronize()
        {
            var localEntriesNames = this.localStorage.Names();

            var cloudEntries = await this.cloudKeyStorage.RetrieveCloudEntriesAsync();
            var cloudEntriesNames = cloudEntries.Keys.ToList();

            var entriesToCompare = cloudEntriesNames.Intersect(localEntriesNames);
            await this.CompareAndUpdateEntries(entriesToCompare, cloudEntries);

            var cloudEntriesToStore = cloudEntriesNames.Except(localEntriesNames);
            this.StoreCloudEntries(cloudEntriesToStore, cloudEntries);

            var localEntriesToDelete = localEntriesNames.Except(cloudEntriesNames);
            this.DeleteLocalEntries(localEntriesToDelete);
            this.IsStorageSynchronized = true;
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
                throw new KeyknoxException($"Entries missing: {string.Join(", ", missingEntryNames)}");
            }

            foreach (var name in names)
            {
                await this.cloudKeyStorage.DeteleEntryAsync(name);
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
            await this.cloudKeyStorage.DeteleAllAsync();
            this.DeleteLocalEntries(this.localStorage.Names());
        }

        public async Task<KeyEntry> StoreEntry(string name, byte[] data, Dictionary<string, string> meta)
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
                if (localEntriesNames.Contains(entry.Name) || this.cloudKeyStorage.ExistsEntry(entry.Name))
                {
                    throw new KeyknoxException($"Entry already exists: #{entry.Name}");
                }
            }

            var keyEntries = new List<KeyEntry>();
            var cloudEntries = await this.cloudKeyStorage.StoreEntries(entries);

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
                    await this.cloudKeyStorage.UpdateEntryAsync(
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
            if (!this.IsStorageSynchronized)
            {
                throw new CloudStorageSyncException("Storage isn't synchronized.");
            }
        }
    }
}
