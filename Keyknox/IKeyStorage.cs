using System;
using System.Collections.Generic;
using Virgil.SDK;

namespace Keyknox
{
    public interface IKeyStorage
    {
        CloudEntry Store(string name, byte[] data, Dictionary<string, string> meta);
        void UpdateEntry(string name, byte[] data, Dictionary<string, string> meta);
        CloudEntry RetrieveEntry(string name);
        CloudEntry[] RetrieveAllEntries();
        void DeteleEntry(string name);
        bool ExistsEntry(string name);
    }
}
