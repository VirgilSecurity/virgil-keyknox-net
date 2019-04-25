using System;
using Virgil.SDK;

namespace Keyknox
{
    public class LocalKeyStorage : KeyStorage
    {
        public readonly string Identity;
        private const string StorageIdentity = "Keyknox";

#if OSX
        public LocalKeyStorage(string identity)
        {
            this.Identity = identity;
            SecureStorage.StorageIdentity = StorageIdentity;
            this.coreStorage = new SecureStorage(identity);
        }
#else
        public LocalKeyStorage(
            string identity,
            string password)
        {
            this.Identity = identity;
            SecureStorage.StorageIdentity = StorageIdentity;
            this.coreStorage = new SecureStorage(password, identity);
        }
#endif
    }
}
