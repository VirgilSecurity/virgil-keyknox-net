using System;
using System.Threading.Tasks;

namespace Keyknox.Client
{
    public interface IKeyknoxClient
    {
        Task<DecryptedKeyknoxValue> ResetValueAsync(string token);
        Task<EncryptedKeyknoxValue> PullValueAsync(string token);
        Task<EncryptedKeyknoxValue> PushValueAsync(byte[] meta, byte[] value, byte[] previousHash, string token);
    }
}
