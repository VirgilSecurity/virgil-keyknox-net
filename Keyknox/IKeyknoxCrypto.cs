using System;
using Virgil.CryptoAPI;

namespace Keyknox
{
    public interface IKeyknoxCrypto
    {
        DetachedEncryptionResult Encrypt(byte[] data, IPrivateKey privateKey, IPublicKey[] publicKeys);
        DecryptedKeyknoxValue Decrypt(EncryptedKeyknoxValue encryptedKeyknoxValue, IPrivateKey privateKey, IPublicKey[] publicKeys);
    }
}
