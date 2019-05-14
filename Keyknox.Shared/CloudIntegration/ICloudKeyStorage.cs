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
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Virgil.CryptoAPI;
    using Virgil.SDK;

    /// <summary>
    /// This interface describes operations needed for cloud storage.
    /// </summary>
    public interface ICloudKeyStorage
    {
        /// <summary>
        /// Whether the storage was synchronized.
        /// </summary>
        /// <returns><c>true</c>, if storage was synchronized, <c>false</c> otherwise.</returns>
        bool IsSynchronized();

        /// <summary>
        /// Uploads all cloud entries from the cloud.
        /// </summary>
        /// <returns>Uploaded cloud entries and their names.</returns>
        Task<Dictionary<string, CloudEntry>> UploadAllAsync();

        /// <summary>
        /// Deletes entry with the specified name from the cloud.
        /// </summary>
        /// <param name="name">Entry name.</param>
        Task DeteleAsync(string name);

        /// <summary>
        /// Deletes all entries from the cloud.
        /// </summary>
        Task DeteleAllAsync();

        /// <summary>
        /// Checks if entry with the specified name exists in the cloud.
        /// </summary>
        /// <returns>True if exists.</returns>
        /// <param name="name">Cloud entry name.</param>
        bool Exists(string name);

        /// <summary>
        /// Retrieves all entries from the cloud.
        /// </summary>
        /// <returns>All cloud entries.</returns>
        List<CloudEntry> RetrieveAll();

        /// <summary>
        /// Retrieves the cloud entry.
        /// </summary>
        /// <returns>The stored cloud entry.</returns>
        /// <param name="name">Cloud entry name.</param>
        CloudEntry Retrieve(string name);

        /// <summary>
        /// Stores an entry with the specified name, data and meta in the cloud.
        /// </summary>
        /// <returns>The saved cloud entry.</returns>
        /// <param name="name">Entry name.</param>
        /// <param name="data">Data to be stored.</param>
        /// <param name="meta">Meta to be stored.</param>
        Task<CloudEntry> StoreAsync(string name, byte[] data, IDictionary<string, string> meta);

        /// <summary>
        /// Stores the specified key entries in the cloud.
        /// </summary>
        /// <returns>Saved cloud entries.</returns>
        /// <param name="keyEntries">Key entries to be stored.</param>
        Task<List<CloudEntry>> StoreAsync(List<KeyEntry> keyEntries);

        /// <summary>
        /// Updates a cloud entry with the specified name.
        /// </summary>
        /// <returns>The updated cloud entry.</returns>
        /// <param name="name">Name of entry to be updated.</param>
        /// <param name="data">New data to be stored in the entry with the specified name.</param>
        Task<CloudEntry> UpdateAsync(string name, byte[] data, IDictionary<string, string> meta = null);

        /// <summary>
        /// Updates public keys for ecnryption and signature verification
        /// and private key for decryption and signature generation
        /// </summary>
        /// <param name="publicKeys">New public keys for ecnryption and signature verification.</param>
        /// <param name="privateKey">New private key for decryption and signature generation.</param>
        /// <returns>Encrypted and signed by specified keys <see cref="T:Keyknox.DecryptedKeyknoxValue"/></returns>
        Task<DecryptedKeyknoxValue> UpdateRecipientsAsync(IPublicKey[] publicKeys, IPrivateKey privateKey);
    }
}
