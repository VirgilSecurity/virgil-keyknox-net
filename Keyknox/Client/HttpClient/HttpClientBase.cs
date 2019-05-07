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
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;
    using Virgil.SDK.Common;
    using Virgil.SDK.Web.Connection;

    public class HttpClientBase
    {
        private const string PreviousHashHeaderAlias = "Virgil-Keyknox-Previous-Hash";
        private const string HashHeaderAlias = "Virgil-Keyknox-Hash";
        private const string DefaultServiceUrl = "https://api.virgilsecurity.com/";
        private readonly IJsonSerializer serializer;
        private HttpClient client;
        private string virgilInfo;

        /// <summary>
        /// Initializes a new instance of the <see cref="HttpClientBase"/> class.
        /// </summary>
        protected HttpClientBase(
            IJsonSerializer serializer,
            string serviceUrl)
        {
            this.serializer = serializer;
            this.client = new HttpClient();
            this.BaseUri = new Uri(serviceUrl ?? DefaultServiceUrl);
            this.client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            this.virgilInfo = VirgilStatInfo();
        }

        /// <summary>
        /// Gets or sets the base URI.
        /// </summary>
        public Uri BaseUri { get; set; }

        protected async Task<BodyModel> SendAsync(
            HttpMethod method, string endpoint, string token, BodyModel body)
        {
            var request = this.NewRequest(method, endpoint, token);

            if (method != HttpMethod.Get)
            {
                var serializedBody = this.serializer.Serialize(body);
                request.Content = new StringContent(serializedBody, Encoding.UTF8, "application/json");
                request.Headers.TryAddWithoutValidation(PreviousHashHeaderAlias, body.KeyknoxHash ?? string.Empty);
            }

            var response = await this.client.SendAsync(request).ConfigureAwait(false);
            return await this.TryParseModel(response);
        }

        protected async Task<BodyModel> SendAsync(HttpMethod method, string endpoint, string token)
        {
            var request = this.NewRequest(method, endpoint, token);

            var response = await this.client.SendAsync(request);
            return await this.TryParseModel(response);
        }

        private static string VirgilStatInfo()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
            string version = fileVersionInfo.ProductVersion;
            return $"Keyknox c# ${Environment.OSVersion} ${version}";
        }

        private HttpRequestMessage NewRequest(HttpMethod method, string endpoint, string appToken)
        {
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                throw new ArgumentException(nameof(endpoint));
            }

            if (string.IsNullOrWhiteSpace(appToken))
            {
                throw new ArgumentException(nameof(appToken));
            }

            Uri endpointUri = this.BaseUri != null
                                  ? new Uri(this.BaseUri, endpoint)
                                  : new Uri(endpoint);

            var request = new HttpRequestMessage(method, endpointUri);

            if (!string.IsNullOrWhiteSpace(appToken))
            {
                request.Headers.TryAddWithoutValidation("Authorization", $"Virgil {appToken}");
            }

            request.Headers.TryAddWithoutValidation("Virgil-Agent", this.virgilInfo);

            return request;
        }

        private void HandleError(HttpStatusCode statusCode, string body)
        {
            string errorMessage;

            switch (statusCode)
            {
                case HttpStatusCode.OK: // OK
                case HttpStatusCode.Created: // Created
                case HttpStatusCode.Accepted: // Accepted
                case HttpStatusCode.NonAuthoritativeInformation: // Non-Authoritative Information
                case HttpStatusCode.NoContent: // No Content
                    return;

                case HttpStatusCode.BadRequest: errorMessage = "Request Error"; break;
                case HttpStatusCode.Unauthorized: errorMessage = "Authorization Error"; break;
                case HttpStatusCode.NotFound: errorMessage = "Entity Not Found"; break;
                case HttpStatusCode.MethodNotAllowed: errorMessage = "Method Not Allowed"; break;
                case HttpStatusCode.InternalServerError: errorMessage = "Internal Server Error"; break;

                default:
                    errorMessage = $"Undefined Exception (Http Status Code: {statusCode})";
                    break;
            }

            uint errorCode = 0;

            if (!string.IsNullOrWhiteSpace(body))
            {
                var error = this.serializer.Deserialize<KeyknoxServiceError>(body);

                errorCode = error?.ErrorCode ?? 0;

                if (error != null && error.Message != null)
                {
                    errorMessage += $": {error.Message}";
                }
            }

            throw new ServiceClientException(errorCode, errorMessage);
        }

        private async Task<BodyModel> TryParseModel(HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();

            this.HandleError(response.StatusCode, content);

            var model = this.serializer.Deserialize<BodyModel>(content);

            IEnumerable<string> previousHashHeader;
            response.Headers.TryGetValues(HashHeaderAlias, out previousHashHeader);

            var enumerator = previousHashHeader.GetEnumerator();
            enumerator.MoveNext();
            model.KeyknoxHash = enumerator.Current;
            return model;
        }
    }
}