// http://stackoverflow.com/a/1344255/2061103

using System.Security.Cryptography;
using System.Text;

namespace HiddenBitcoin.Helpers
{
    public class KeyGenerator
    {
        public static string GetUniqueKey(int maxSize)
        {
            var chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890".ToCharArray();
            var data = new byte[1];
            using (var crypto = new RNGCryptoServiceProvider())
            {
                crypto.GetNonZeroBytes(data);
                data = new byte[maxSize];
                crypto.GetNonZeroBytes(data);
            }
            var result = new StringBuilder(maxSize);
            foreach (var b in data)
            {
                result.Append(chars[b%(chars.Length)]);
            }
            return result.ToString();
        }
    }
}