using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Keyknox.Client;
using Keyknox.Utils;
using Virgil.CryptoAPI;
using Virgil.SDK.Web.Authorization;

namespace Keyknox
{
    public class KeyknoxManager
    {
        private IKeyknoxCrypto crypto;
        private IAccessTokenProvider accessTokenProvider;
        private IKeyknoxClient keyknoxClient;
        private IPublicKey[] publicKeys;
        private IPrivateKey privateKey;
        
        public KeyknoxManager(IAccessTokenProvider accessTokenProvider,
                              IPrivateKey privateKey, 
                              IPublicKey[] publicKeys,
                              IKeyknoxClient keyknoxClient = null, 
                              IKeyknoxCrypto keyknoxCrypto = null)
        {
            this.crypto = keyknoxCrypto ?? new KeyknoxCrypto();
            this.accessTokenProvider = accessTokenProvider;
            this.privateKey = privateKey;
            this.publicKeys = publicKeys;
            this.keyknoxClient = keyknoxClient ?? new KeyknoxClient(new NewtonsoftJsonSerializer());
        }

        public async Task<DecryptedKeyknoxValue> PushValueAsync(byte[] data,
                                                                byte[] previoushash){
            var token = await accessTokenProvider.GetTokenAsync(new TokenContext(null, "put", false, "keyknox"));
            var detachedEncryptionResult = crypto.Encrypt(data, privateKey, publicKeys);
            var encryptedKeyknoxVal = await keyknoxClient.PushValueAsync(detachedEncryptionResult.Meta,
                                                                         detachedEncryptionResult.Value,
                                                                         previoushash,
                                                                         token.ToString());
            return crypto.Decrypt(encryptedKeyknoxVal, privateKey, publicKeys);
        }

        public async Task<DecryptedKeyknoxValue> PullValueAsync()
        {
            var token = await accessTokenProvider.GetTokenAsync(new TokenContext(null, "get", false, "keyknox"));
            var encryptedKeyknoxVal = await keyknoxClient.PullValueAsync(token.ToString());
            return crypto.Decrypt(encryptedKeyknoxVal, privateKey, publicKeys);
        }

        public async Task<DecryptedKeyknoxValue> ResetValueAsync()
        {
            var token = await accessTokenProvider.GetTokenAsync(new TokenContext(null, "delete", false, "keyknox"));
            var decryptedKeyknoxVal = await keyknoxClient.ResetValueAsync(token.ToString());
            return decryptedKeyknoxVal;
        }

        public async Task<DecryptedKeyknoxValue> UpdateRecipients(IPublicKey[] newPublicKeys,
                                                                  IPrivateKey newPrivateKey = null)
        {
            // todo exception if empty list of pblickeys
            var decryptedKeyknoxVal = await PullValueAsync();
            if (decryptedKeyknoxVal.Meta == null || decryptedKeyknoxVal.Meta.Length == 0 
                || decryptedKeyknoxVal.Value == null || decryptedKeyknoxVal.Value.Length == 0){
                return decryptedKeyknoxVal;
            }
            this.privateKey = newPrivateKey ?? this.privateKey;
            this.publicKeys = newPublicKeys;

            return await PushValueAsync(decryptedKeyknoxVal.Value, decryptedKeyknoxVal.KeyknoxHash);
           
        }

        public async Task<DecryptedKeyknoxValue> UpdateRecipientsAndPushValue(byte[] data,
                                                                  byte[] previoushash,
                                                                  IPublicKey[] newPublicKeys = null,
                                                                  IPrivateKey newPrivateKey = null)
        {
            this.privateKey = newPrivateKey ?? this.privateKey;
            this.publicKeys = newPublicKeys;
            return await PushValueAsync(data, previoushash);
        }

    }
}
