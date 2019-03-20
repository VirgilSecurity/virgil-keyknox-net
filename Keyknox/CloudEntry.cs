using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Keyknox
{
    [DataContract]
    public class CloudEntry
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "data")]
        public byte[] Data { get; set; }

        [DataMember(Name = "creation_date")]
        public DateTime CreationDate { get; set; }

        [DataMember(Name = "modification_date")]
        public DateTime ModificationDate { get; set; }

        [DataMember(Name = "meta", EmitDefaultValue = false)]
        public Dictionary<string, string> Meta { get; set; }
    }
}
