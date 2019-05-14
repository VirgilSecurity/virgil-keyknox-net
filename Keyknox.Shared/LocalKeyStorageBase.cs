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
    using Virgil.SDK;

    public class LocalKeyStorageBase : KeyStorage, ILocalKeyStorage
    {
        protected const string StorageIdentity = "Keyknox";

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Keyknox.LocalKeyStorageBase"/> class.
        /// <param name="identity">User's identity to group keys in local storage.</param>
        /// <param name="coreStorage">Secure storage from Virgil.SDK</param>
        /// </summary>
        public LocalKeyStorageBase(string identity, SecureStorage coreStorage)
        {
            if (string.IsNullOrWhiteSpace(identity))
            {
                throw new ArgumentException("Storage identity should not be empty");
            }

            this.Identity = identity;
            SecureStorage.StorageIdentity = StorageIdentity;
            this.coreStorage = coreStorage ?? throw new ArgumentException("Secure Storage should not be null");
        }

        /// <summary>
        /// User's identity to group keys in local storage.
        /// </summary>
        /// <value>The identity.</value>
        public string Identity { get; protected set; }

        /// <summary>
        /// Load key entry by the specified name.
        /// </summary>
        /// <returns>The loaded key entry.</returns>
        /// <param name="name">Key entry name.</param>
        public new KeyEntry Load(string name)
        {
            var entry = base.Load(name ?? throw new ArgumentException(nameof(name)));
            entry.Meta = MetaDate.DeleteFrom(entry.Meta);
            return entry;
        }

        /// <summary>
        /// Load key entry with creation and modification dates by the specified name.
        /// </summary>
        /// <returns>The loaded key entry.</returns>
        /// <param name="name">Key entry name.</param>
        public KeyEntry LoadFull(string name)
        {
            return base.Load(name ?? throw new ArgumentException(nameof(name)));
        }

        /// <summary>
        /// Store the specified keyEntry, creationDate and modificationDate.
        /// </summary>
        /// <returns>The stored key entry.</returns>
        /// <param name="keyEntry">Key entry.</param>
        /// <param name="creationDate">Creation date.</param>
        /// <param name="modificationDate">Modification date.</param>
        public KeyEntry Store(KeyEntry keyEntry, DateTime creationDate, DateTime modificationDate)
        {
            this.ValidateStoreParams(keyEntry, creationDate, modificationDate);

            var meta = keyEntry.Meta ?? new Dictionary<string, string>();
            var newKeyEntry = new KeyEntry()
            {
                Name = keyEntry.Name,
                Value = keyEntry.Value,
                Meta = MetaDate.AppendTo(
                    meta,
                    creationDate,
                    modificationDate)
            };
            base.Store(newKeyEntry);

            return newKeyEntry;
        }

        /// <summary>
        /// Store the specified key entry.
        /// </summary>
        /// <returns>The stored key entry.</returns>
        /// <param name="keyEntry">Key entry to be stored.</param>
        public new KeyEntry Store(KeyEntry keyEntry)
        {
            return this.Store(
                keyEntry,
                DateTime.UtcNow.RoundTicks(),
                DateTime.UtcNow.RoundTicks());
        }

        private void ValidateStoreParams(KeyEntry keyEntry, DateTime creationDate, DateTime modificationDate)
        {
            if (keyEntry == null)
            {
                throw new ArgumentException(nameof(keyEntry));
            }

            if (creationDate == null)
            {
                throw new ArgumentException(nameof(creationDate));
            }

            if (modificationDate == null)
            {
                throw new ArgumentException(nameof(modificationDate));
            }
        }
    }
}
