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

        public async Task StoreEntry(string name, byte[] data, Dictionary<string, string> meta){
            
        }
        public async Task SynchronizeStoragesAsync()
        {
            var cloudEntries = await this.cloudKeyStorage.RetrieveCloudEntries();
            //var cloudEntries = this.cloudKeyStorage.RetrieveAllEntries();
            var cloudEntriesNames = cloudEntries.Keys.ToList();
          //  cloudEntries.ForEach(entry => cloudEntriesNames.Add(entry.Name));

            var localEntriesNames = this.localStorage.Names();
            var localEntriesToDelete = localEntriesNames.Except(cloudEntriesNames);

            DeleteLocalEntries(localEntriesToDelete);
            var cloudEntriesToStore = cloudEntriesNames.Except(localEntriesNames);
            StoreCloudEntries(cloudEntriesToStore, cloudEntries);

            var EntriesToCompare = cloudEntriesNames.Intersect(localEntriesNames);
            CompareAndUpdateEntries(EntriesToCompare, cloudEntries);
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

        private void CompareAndUpdateEntries(IEnumerable<string> names, Dictionary<string, CloudEntry> cloudEntries){
            foreach (var name in names)
            {
                var localKeyEntry = this.localStorage.Load(name);
                var cloudEntry = cloudEntries[name];
                // todo compare creation data in meta
                //this.localStorage.Store(
                //    new KeyEntry { Value = cloudEntries[name].Data, Name = cloudEntries[name].Name, Meta = cloudEntries[name].Meta }
                //);
            }
        }
    }
}
