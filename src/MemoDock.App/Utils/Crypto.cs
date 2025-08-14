using System.Security.Cryptography;
using System.Text;

namespace MemoDock.App.Utils
{
    public static class Crypto
    {
        public static string Sha256Hex(string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text ?? string.Empty);
            return Sha256Hex(bytes);
        }

        public static string Sha256Hex(byte[] data)
        {
            using var sha = SHA256.Create();
            return Convert.ToHexString(sha.ComputeHash(data)).ToLowerInvariant();
        }

        public static string Sha256(string text) => Sha256Hex(text);

        // placeholdery – aktualnie nie szyfrujemy
        public static byte[] Encrypt(byte[] data, string? password = null) => data;
        public static byte[] Decrypt(byte[] data, string? password = null) => data;
    }
}
