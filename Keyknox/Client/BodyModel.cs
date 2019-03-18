using System;
using System.Runtime.Serialization;

namespace Keyknox.Client
{
    [DataContract]
    public class BodyModel
    {
        [DataMember(Name = "meta")]
        public string Meta { get; set; }

        [DataMember(Name = "value")]
        public string Value { get; set; }

        [DataMember(Name = "version", EmitDefaultValue = false)]
        public String Version { get; set; }

        public string KeyknoxHash { get; set; }
    }
}
