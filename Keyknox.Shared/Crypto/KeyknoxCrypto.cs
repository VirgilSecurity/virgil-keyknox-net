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
    using System.Linq;
    using Virgil.Crypto;
    using Virgil.CryptoAPI;

    /// <summary>
    /// Keyknox crypto has crypto operations needed for Keyknox.
    /// </summary>
    public class KeyknoxCrypto : IKeyknoxCrypto
    {
        private readonly VirgilCrypto crypto;

        public KeyknoxCrypto()
        {
            this.crypto = new VirgilCrypto();
        }

        /// <summary>
        /// Encrypt and sign the specified data by specified privateKey and publicKeys.
        /// </summary>
        /// <returns>The encrypted data and meta information.</returns>
        /// <param name="data">Data to be encrypted.</param>
        /// <param name="privateKey">Private key to generate signature.</param>
        /// <param name="publicKeys">Public keys to encrypt data.</param>
        public DetachedEncryptionResult Encrypt(byte[] data, IPrivateKey privateKey, IPublicKey[] publicKeys)
        {
            ValidatePublicKeys(publicKeys);
            ValidatePrivateKey(privateKey);

            return this.crypto.SignThenEncryptDetached(
                data ?? throw new ArgumentNullException(nameof(data)),
                privateKey,
                publicKeys);
        }

        /// <summary>
        /// Decrypt and verify the specified encryptedKeyknoxValue using specified privateKey and publicKeys.
        /// </summary>
        /// <returns>The decrypted data and meta information.</returns>
        /// <param name="encryptedKeyknoxValue">Encrypted keyknox value.</param>
        /// <param name="privateKey">Private key.</param>
        /// <param name="publicKeys">Public keys.</param>
        public DecryptedKeyknoxValue Decrypt(EncryptedKeyknoxValue encryptedKeyknoxValue, IPrivateKey privateKey, IPublicKey[] publicKeys)
        {
            if (encryptedKeyknoxValue == null)
            {
                throw new ArgumentNullException(nameof(encryptedKeyknoxValue));
            }

            if ((encryptedKeyknoxValue.Meta == null || !encryptedKeyknoxValue.Meta.Any()) &&
                (encryptedKeyknoxValue.Value == null || !encryptedKeyknoxValue.Value.Any()))
            {
                return new DecryptedKeyknoxValue()
                {
                    Value = new byte[0],
                    Meta = new byte[0],
                    Version = encryptedKeyknoxValue.Version,
                    KeyknoxHash = encryptedKeyknoxValue.KeyknoxHash
                };
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
                Meta = encryptedKeyknoxValue.Meta,
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
