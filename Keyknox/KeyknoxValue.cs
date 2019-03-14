using System;
namespace Keyknox
{
    public class KeyknoxValue
    {
        public byte[] Meta { get; set; }
        public byte[] Value { get; set; }
        public String Version { get; set; }
        public byte[] KeyknoxHash { get; set; }
    }
}
