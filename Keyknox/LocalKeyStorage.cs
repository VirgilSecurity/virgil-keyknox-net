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

    public class LocalKeyStorage : KeyStorage
    {
        private const string StorageIdentity = "Keyknox";

#if OSX
        public LocalKeyStorage(string identity)
        {
            this.Identity = identity;
            SecureStorage.StorageIdentity = StorageIdentity;
            this.coreStorage = new SecureStorage(identity);
        }

#else
        public LocalKeyStorage(
            string identity,
            string password)
        {
            this.Identity = identity;
            SecureStorage.StorageIdentity = StorageIdentity;
            this.coreStorage = new SecureStorage(password, identity);
        }
#endif
        public string Identity { get; private set; }

        public new KeyEntry Load(string name)
        {
            var entry = base.Load(name);
            entry.Meta = MetaDate.DeleteFrom(entry.Meta);
            return entry;
        }

        public KeyEntry LoadFull(string name)
        {
            return base.Load(name);
        }

        public KeyEntry Store(KeyEntry keyEntry, DateTime creationDate, DateTime modificationDate)
        {
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

        public new KeyEntry Store(KeyEntry keyEntry)
        {
            return this.Store(
                keyEntry,
                DateTime.UtcNow.RoundTicks(),
                DateTime.UtcNow.RoundTicks());
        }
    }
}
