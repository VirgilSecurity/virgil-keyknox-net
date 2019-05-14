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
    using System.Threading.Tasks;
    using Virgil.CryptoAPI;

    public interface IKeyknoxManager
    {
        /// <summary>
        /// Signs, encrypts and then pushes data to Keyknox service
        /// </summary>
        /// <returns>Decrypted and then verified data from Keyknox service.</returns>
        /// <param name="data">Data to be pushed.</param>
        /// <param name="previoushash">Previous data hash.</param>
        Task<DecryptedKeyknoxValue> PushValueAsync(byte[] data, byte[] previoushash = null);

        /// <summary>
        /// Pulls the value from Keyknox service and then decrypts and verifies it.
        /// </summary>
        /// <returns>Decrypted and verified data from Keyknox service.</returns>
        Task<DecryptedKeyknoxValue> PullValueAsync();

        /// <summary>
        /// Resets the value in the cloud and increments its version.
        /// </summary>
        /// <returns>Decrypted keyknox value.</returns>
        Task<DecryptedKeyknoxValue> ResetValueAsync();

        /// <summary>
        /// Updates public keys for ecnryption and signature verification
        /// and private key for decryption and signature generation
        /// </summary>
        /// <param name="newPublicKeys">New public keys for ecnryption and signature verification.</param>
        /// <param name="newPrivateKey">New private key for decryption and signature generation.</param>
        /// <returns>Encrypted and signed by specified keys <see cref="T:Keyknox.DecryptedKeyknoxValue"/></returns>
        Task<DecryptedKeyknoxValue> UpdateRecipientsAsync(IPublicKey[] newPublicKeys, IPrivateKey newPrivateKey = null);

        /// <summary>
        /// Updates public keys for ecnryption and signature verification
        /// and private key for decryption and signature generation and pushes new value to Keyknox service.
        /// </summary>
        /// <param name="newPublicKeys">New public keys for ecnryption and signature verification.</param>
        /// <param name="newPrivateKey">New private key for decryption and signature generation.</param>
        Task<DecryptedKeyknoxValue> UpdateRecipientsAsync(
            byte[] data,
            byte[] previoushash,
            IPublicKey[] newPublicKeys = null,
            IPrivateKey newPrivateKey = null);
    }
}