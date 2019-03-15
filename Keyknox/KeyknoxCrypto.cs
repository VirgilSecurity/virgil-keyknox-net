using System;
using Virgil.Crypto;
using Virgil.Crypto.Foundation;
using Virgil.CryptoAPI;

namespace Keyknox
{
    public class KeyknoxCrypto : IKeyknoxCrypto
    {
        private readonly VirgilCrypto crypto;

        public KeyknoxCrypto(){
            crypto = new VirgilCrypto();
        }

        public DetachedEncryptionResult Encrypt(byte[] data, IPrivateKey privateKey, IPublicKey[] publicKeys)
        {
            var encrypted = this.crypto.SignThenEncryptDetached(data, privateKey, publicKeys);
            return new DetachedEncryptionResult(){
                Meta = encrypted.Meta,
                Value = encrypted.Value
            };
        }

        public DecryptedKeyknoxValue Decrypt(EncryptedKeyknoxValue encryptedKeyknoxValue, IPrivateKey privateKey, IPublicKey[] publicKeys)
        {
            //todo validate
            var decrypted = this.crypto.DecryptThenVerifyDetached(encryptedKeyknoxValue.Value, 
                                                                  encryptedKeyknoxValue.Meta,
                                                                  privateKey, publicKeys);  
            return new DecryptedKeyknoxValue() {
                Value = decrypted,
                Version = encryptedKeyknoxValue.Version,
                KeyknoxHash = encryptedKeyknoxValue.KeyknoxHash 
            };
        }
    }
}
