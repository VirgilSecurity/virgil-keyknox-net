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

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Keyknox.Client;
using Keyknox.Utils;
using Virgil.CryptoAPI;
using Virgil.SDK.Common;
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
