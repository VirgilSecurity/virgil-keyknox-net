using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Virgil.SDK;

namespace Keyknox
{
    public class SyncKeyStorage{
        private string identity;
        private CloudKeyStorage cloudKeyStorage;
        private KeyStorage localStorage;
        public SyncKeyStorage(string identity, CloudKeyStorage cloudKeyStorage, KeyStorage localStorage){
            this.identity = identity;
            this.cloudKeyStorage = cloudKeyStorage;
            this.localStorage = localStorage;
        }


        public KeyEntry RetrieveEntry(string name){
            return this.localStorage.Load(name);
        }

        public async Task DeleteEntriesAsync(List<string> names){
            var localEntriesNames = new List<string>(this.localStorage.Names());
            var missingEntryNames = names.Except(localEntriesNames);
            if (missingEntryNames.Any()){
                throw new Exception($"missing keys: {String.Join(", ", missingEntryNames) }");
            }

            foreach (var name in names)
            {
                await this.cloudKeyStorage.DeteleEntryAsync(name);
                this.localStorage.Delete(name);
            }
        }
        public bool ExistsEntry(string name){
            return localStorage.Exists(name);
        }

        public List<KeyEntry> RetrieveAllEntries(){
            var localEntriesNames = this.localStorage.Names();
            var keyEntries = new List<KeyEntry>();
            foreach (var name in localEntriesNames)
            {
                keyEntries.Add(this.localStorage.Load(name));
            }
            return keyEntries;
        }

        public async Task DeleteEntryAsync(string name){
            await DeleteEntriesAsync(new List<string>(){ name });
        }

        public async Task DeleteAllEntries(){
            await this.cloudKeyStorage.DeteleAllAsync();
            DeleteLocalEntries(this.localStorage.Names());
        }

        public async Task<KeyEntry> StoreEntry(string name, byte[] data, Dictionary<string, string> meta){
                var keyEntry = new KeyEntry() { Name = name, Meta = meta, Value = data };
            var storedEntries = await StoreEntries(new List<KeyEntry>() { keyEntry });
            return storedEntries.First();
        }

        public async Task<List<KeyEntry>> StoreEntries(List<KeyEntry> entries)
        {
            var localEntriesNames = new List<string>(this.localStorage.Names());
            foreach (var entry in entries)
            {
                if (localEntriesNames.Contains(entry.Name) || this.cloudKeyStorage.ExistsEntry(entry.Name)){
                    throw new Exception("already exist"); 
                }
            }
            var keyEntries = new List<KeyEntry>();
            var cloudEntries = await this.cloudKeyStorage.StoreEntries(entries);

            foreach (var entry in cloudEntries)
            {
                KeyEntry keyEntry = CloneFromCloudToLocal(entry);
                keyEntries.Add(keyEntry);
            }

            return keyEntries;
        }

        private KeyEntry CloneFromCloudToLocal(CloudEntry entry)
        {
            var meta = new Dictionary<string, string>(entry.Meta);
            string format = "MMM ddd d HH:mm yyyy";
            meta.Add("keyknox_crd", entry.CreationDate.ToString(format));
            meta.Add("keyknox_upd", entry.ModificationDate.ToString(format));

            var keyEntry = new KeyEntry()
            {
                Name = entry.Name,
                Value = entry.Data,
                Meta = meta
            };
            this.localStorage.Store(keyEntry);
            return keyEntry;
        }

        public async Task SynchronizeStoragesAsync()
        {
            var localEntriesNames = this.localStorage.Names();

            var cloudEntries = await this.cloudKeyStorage.RetrieveCloudEntries();
            var cloudEntriesNames = cloudEntries.Keys.ToList();

            var EntriesToCompare = cloudEntriesNames.Intersect(localEntriesNames);
            await CompareAndUpdateEntries(EntriesToCompare, cloudEntries);

            var cloudEntriesToStore = cloudEntriesNames.Except(localEntriesNames);
            StoreCloudEntries(cloudEntriesToStore, cloudEntries);

            var localEntriesToDelete = localEntriesNames.Except(cloudEntriesNames);
            DeleteLocalEntries(localEntriesToDelete);

        }

        private void DeleteLocalEntries(IEnumerable<string> names){
            foreach (var name in names){
                this.localStorage.Delete(name);
            }

        }

        private void StoreCloudEntries(IEnumerable<string> names, Dictionary<string, CloudEntry> cloudEntries)
        {
            foreach (var name in names)
            {
                this.localStorage.Store(
                    new KeyEntry { Value = cloudEntries[name].Data, Name = cloudEntries[name].Name, Meta = cloudEntries[name].Meta }
                );
            }
        }

        private async Task CompareAndUpdateEntries(IEnumerable<string> names, Dictionary<string, CloudEntry> cloudEntries){
            foreach (var name in names)
            {
                var localKeyEntry = this.localStorage.Load(name);
                var cloudEntry = cloudEntries[name];
                string format = "MMM ddd d HH:mm yyyy";
                var modificationDate = DateTime.ParseExact(localKeyEntry.Meta["keyknox_upd"], format, null);

                if (modificationDate < cloudEntry.ModificationDate){
                    this.localStorage.Delete(name);
                    CloneFromCloudToLocal(cloudEntry);
                }

                if (modificationDate > cloudEntry.ModificationDate)
                {
                    await this.cloudKeyStorage.UpdateEntryAsync(name,
                                                                localKeyEntry.Value,
                                                                (Dictionary<string, string>)localKeyEntry.Meta);
                    this.localStorage.Delete(name);
                    CloneFromCloudToLocal(cloudEntry);
                }
            }
        }
    }
}
