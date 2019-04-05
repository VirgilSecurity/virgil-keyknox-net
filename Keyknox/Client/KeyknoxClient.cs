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

namespace Keyknox.Client
{
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Virgil.SDK.Common;

    public class KeyknoxClient : HttpClientBase, IKeyknoxClient
    {
        public KeyknoxClient(IJsonSerializer serializer, string serviceUrl = null)
            : base(serializer, serviceUrl)
        {
        }

        public async Task<EncryptedKeyknoxValue> PullValueAsync(string token)
        {
            var response = await this.SendAsync(
                HttpMethod.Get, $"keyknox/v1", token).ConfigureAwait(false);

            return new EncryptedKeyknoxValue()
            {
                Value = Bytes.FromString(response.Value, StringEncoding.BASE64),
                Meta = Bytes.FromString(response.Meta, StringEncoding.BASE64),
                Version = response.Version,
                KeyknoxHash = Bytes.FromString(response.KeyknoxHash, StringEncoding.BASE64)
            };
        }

        public async Task<EncryptedKeyknoxValue> PushValueAsync(byte[] meta, byte[] value, byte[] previousHash, string token)
        {
            var model = new BodyModel()
            {
                Meta = Bytes.ToString(meta, StringEncoding.BASE64),
                Value = Bytes.ToString(value, StringEncoding.BASE64),
            };
            if (previousHash != null)
            {
                model.KeyknoxHash = Bytes.ToString(previousHash, StringEncoding.BASE64);
            }

            var response = await this.SendAsync(
                HttpMethod.Put, $"keyknox/v1", token, model).ConfigureAwait(false);

            return new EncryptedKeyknoxValue()
            {
                Value = Bytes.FromString(response.Value, StringEncoding.BASE64),
                Meta = Bytes.FromString(response.Meta, StringEncoding.BASE64),
                Version = response.Version,
                KeyknoxHash = Bytes.FromString(response.KeyknoxHash, StringEncoding.BASE64)
            };
        }

        public async Task<DecryptedKeyknoxValue> ResetValueAsync(string token)
        {
            var response = await this.SendAsync(
                HttpMethod.Post, $"keyknox/v1/reset", token).ConfigureAwait(false);

            return new DecryptedKeyknoxValue()
            {
                Value = Bytes.FromString(response.Value, StringEncoding.BASE64),
                Meta = Bytes.FromString(response.Meta, StringEncoding.BASE64),
                Version = response.Version,
                KeyknoxHash = Bytes.FromString(response.KeyknoxHash, StringEncoding.BASE64)
            };
        }
    }
}
