using System;
using System.Collections.Generic;
using Keyknox.Utils;
using Virgil.SDK.Common;

namespace Keyknox
{
    public class CloudKeyCache
    {
        public Dictionary<string, CloudEntry> Entries{ get; private set; }
        public DecryptedKeyknoxValue Response{ get; private set; }
        private ICloudSerializer serializer;

        public CloudKeyCache(DecryptedKeyknoxValue keyknoxResponse, ICloudSerializer serializer = null)
        {
            this.serializer = serializer ?? new CloudSerializer(new NewtonsoftJsonSerializer());
            Refresh(keyknoxResponse);
        }

        public void Refresh(DecryptedKeyknoxValue keyknoxResponse){
            Response = keyknoxResponse;
            Entries = serializer.Deserialize(Response.Value);
        }
    }
}
