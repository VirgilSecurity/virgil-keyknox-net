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
    using System.Linq;
    using System.Threading.Tasks;
    using Keyknox.Client;
    using Virgil.CryptoAPI;
    using Virgil.SDK.Common;
    using Virgil.SDK.Web.Authorization;

    public class KeyknoxManager
    {
        private IKeyknoxCrypto crypto;
        private IAccessTokenProvider accessTokenProvider;
        private IKeyknoxClient keyknoxClient;
        private IPublicKey[] publicKeys;
        private IPrivateKey privateKey;

        public KeyknoxManager(
            IAccessTokenProvider accessTokenProvider,
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

        public async Task<DecryptedKeyknoxValue> PushValueAsync(
            byte[] data,
            byte[] previoushash = null)
        {
            var token = await this.accessTokenProvider.GetTokenAsync(new TokenContext(null, "put", false, "keyknox"));
            var detachedEncryptionResult = this.crypto.Encrypt(data, this.privateKey, this.publicKeys);
            var encryptedKeyknoxVal = await this.keyknoxClient.PushValueAsync(
                detachedEncryptionResult.Meta,
                detachedEncryptionResult.Value,
                previoushash,
                token.ToString());
            return this.crypto.Decrypt(encryptedKeyknoxVal, this.privateKey, this.publicKeys);
        }

        public async Task<DecryptedKeyknoxValue> PullValueAsync()
        {
            var token = await this.accessTokenProvider.GetTokenAsync(new TokenContext(null, "get", false, "keyknox"));
            var encryptedKeyknoxVal = await this.keyknoxClient.PullValueAsync(token.ToString());
            return this.crypto.Decrypt(encryptedKeyknoxVal, this.privateKey, this.publicKeys);
        }

        public async Task<DecryptedKeyknoxValue> ResetValueAsync()
        {
            var token = await this.accessTokenProvider.GetTokenAsync(new TokenContext(null, "delete", false, "keyknox"));
            var decryptedKeyknoxVal = await this.keyknoxClient.ResetValueAsync(token.ToString());
            return decryptedKeyknoxVal;
        }

        public async Task<DecryptedKeyknoxValue> UpdateRecipientsAsync(
            IPublicKey[] newPublicKeys,
            IPrivateKey newPrivateKey = null)
        {
            CheckPublicKeys(newPublicKeys);

            var decryptedKeyknoxVal = await this.PullValueAsync();
            if (IsEmpty(decryptedKeyknoxVal))
            {
                return decryptedKeyknoxVal;
            }

            this.privateKey = newPrivateKey ?? this.privateKey;
            this.publicKeys = newPublicKeys;
            return await this.PushValueAsync(decryptedKeyknoxVal.Value, decryptedKeyknoxVal.KeyknoxHash);
        }


        public async Task<DecryptedKeyknoxValue> UpdateRecipientsAsync(
            byte[] data,
            byte[] previoushash,
            IPublicKey[] newPublicKeys = null,
            IPrivateKey newPrivateKey = null)
        {
            CheckPublicKeys(newPublicKeys);

            var decryptedKeyknoxVal = await this.PullValueAsync();

            if (IsEmpty(decryptedKeyknoxVal))
            {
                return decryptedKeyknoxVal;
            }

            this.privateKey = newPrivateKey ?? this.privateKey;
            this.publicKeys = newPublicKeys;

            return await this.PushValueAsync(data, previoushash);
        }

        private bool IsEmpty(DecryptedKeyknoxValue decryptedKeyknoxVal)
        {
            return (decryptedKeyknoxVal.Meta == null || decryptedKeyknoxVal.Meta.Length == 0
                     || decryptedKeyknoxVal.Value == null || decryptedKeyknoxVal.Value.Length == 0);
        }

        private static void CheckPublicKeys(IPublicKey[] newPublicKeys)
        {
            if (newPublicKeys == null || !newPublicKeys.Any())
            {
                throw new KeyknoxException("Public key isn't provided");
            }
        }
    }
}
