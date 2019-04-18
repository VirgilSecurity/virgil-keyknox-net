#region Copyright (C) Virgil Security Inc.
// Copyright (C) 2015-2019 Virgil Security Inc.
// 
// Lead Maintainer: Virgil Security Inc. <support@virgilsecurity.com>
// 
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions 
// are met:
// 
//   (1) Redistributions of source code must retain the above copyright
//   notice, this list of conditions and the following disclaimer.
//   
//   (2) Redistributions in binary form must reproduce the above copyright
//   notice, this list of conditions and the following disclaimer in
//   the documentation and/or other materials provided with the
//   distribution.
//   
//   (3) Neither the name of the copyright holder nor the names of its
//   contributors may be used to endorse or promote products derived 
//   from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE AUTHOR ''AS IS'' AND ANY EXPRESS OR
// IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY DIRECT,
// INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
// SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
// HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
// STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING
// IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
// POSSIBILITY OF SUCH DAMAGE.
#endregion

namespace Virgil.SDK.Common
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using System;

    public class NewtonsoftJsonExtendedSerializer : NewtonsoftJsonSerializer
    {
        public NewtonsoftJsonExtendedSerializer()
        {
            base.Settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };

            base.Settings.Converters.Add(new UnixTimestampConverterInMilliseconds());
        }
    }

    public class UnixTimestampConverterInMilliseconds : UnixTimestampConverter
    {
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return reader.Value == null ? base.ReadJson(reader, objectType, existingValue, serializer) : TimeFromUnixTimestampInMilliseconds((long)reader.Value);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteRawValue(UnixTimestampInMillisecondsFromDateTime((DateTime)value).ToString());
        }

        private static DateTime TimeFromUnixTimestampInMilliseconds(long unixTimestamp)
        {
            return TimeFromUnixTimestamp(unixTimestamp / 1000);
        }

        public static long UnixTimestampInMillisecondsFromDateTime(DateTime date)
        {
            return UnixTimestampFromDateTime(date) * 1000;
        }
    }
}
