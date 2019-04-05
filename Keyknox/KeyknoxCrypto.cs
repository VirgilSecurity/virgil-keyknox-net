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
    using Virgil.Crypto;
    using Virgil.CryptoAPI;

    public class KeyknoxCrypto : IKeyknoxCrypto
    {
        private readonly VirgilCrypto crypto;

        public KeyknoxCrypto()
        {
            this.crypto = new VirgilCrypto();
        }

        public DetachedEncryptionResult Encrypt(byte[] data, IPrivateKey privateKey, IPublicKey[] publicKeys)
        {
            ValidatePublicKeys(publicKeys);
            ValidatePrivateKey(privateKey);

            var encrypted = this.crypto.SignThenEncryptDetached(data, privateKey, publicKeys);
            return new DetachedEncryptionResult()
            {
                Meta = encrypted.Meta,
                Value = encrypted.Value
            };
        }

        public DecryptedKeyknoxValue Decrypt(EncryptedKeyknoxValue encryptedKeyknoxValue, IPrivateKey privateKey, IPublicKey[] publicKeys)
        {
            if ((encryptedKeyknoxValue.Meta == null || !encryptedKeyknoxValue.Meta.Any()) &&
                (encryptedKeyknoxValue.Value == null || !encryptedKeyknoxValue.Value.Any()))
            {
                return new DecryptedKeyknoxValue() { Value = new byte[0], Meta = new byte[0] };
            }

            ValidatePublicKeys(publicKeys);
            ValidatePrivateKey(privateKey);

            var decrypted = this.crypto.DecryptThenVerifyDetached(
                encryptedKeyknoxValue.Value,
                encryptedKeyknoxValue.Meta,
                privateKey,
                publicKeys);
            return new DecryptedKeyknoxValue()
            {
                Value = decrypted,
                Version = encryptedKeyknoxValue.Version,
                KeyknoxHash = encryptedKeyknoxValue.KeyknoxHash
            };
        }

        private static void ValidatePublicKeys(IPublicKey[] publicKeys)
        {
            if (publicKeys == null || !publicKeys.Any())
            {
                throw new KeyknoxException("Public key isn't provided");
            }
        }

        private static void ValidatePrivateKey(IPrivateKey privateKey)
        {
            if (privateKey == null)
            {
                throw new KeyknoxException($"Private key isn't provided");
            }
        }
    }
}
