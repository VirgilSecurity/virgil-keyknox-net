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
    using Virgil.SDK;

    /// <summary>
    /// This interface describes operations needed for local storage.
    /// </summary>
    public interface ILocalKeyStorage
    {
        /// <summary>
        /// Load key entry by the specified name.
        /// </summary>
        /// <returns>The loaded key entry.</returns>
        /// <param name="name">Key entry name.</param>
        KeyEntry Load(string name);

        /// <summary>
        /// Store the specified keyEntry, creationDate and modificationDate.
        /// </summary>
        /// <returns>The stored key entry.</returns>
        /// <param name="keyEntry">Key entry.</param>
        /// <param name="creationDate">Creation date.</param>
        /// <param name="modificationDate">Modification date.</param>
        KeyEntry Store(KeyEntry keyEntry, DateTime creationDate, DateTime modificationDate);

        /// <summary>
        /// Load key entry with creation and modification dates by the specified name.
        /// </summary>
        /// <returns>The loaded key entry.</returns>
        /// <param name="name">Key entry name.</param>
        KeyEntry LoadFull(string name);

        /// <summary>
        /// Store the specified keyEntry in the local storage.
        /// </summary>
        /// <returns>The stored key entry.</returns>
        KeyEntry Store(KeyEntry keyEntry);

        /// <summary>
        /// Returns the list of aliases that are kept in the storage.
        /// </summary>
        string[] Names();

        /// <summary>
        /// Delete the instance of <see cref="KeyEntry"/> by the given name.
        /// </summary>
        /// <param name="name">The alias name.</param>
        void Delete(string name);

        /// <summary>
        /// Checks if the given alias exists in this storage.
        /// </summary>
        /// <param name="name">The alias name.</param>
        bool Exists(string name);
    }
}