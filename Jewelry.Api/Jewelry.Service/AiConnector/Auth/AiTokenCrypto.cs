using System;
using System.Security.Cryptography;
using System.Text;

namespace Jewelry.Service.AiConnector.Auth
{
    public static class AiTokenCrypto
    {
        // random 256 bit, base64url
        public static string RandomToken(int bytes = 32)
        {
            var buf = RandomNumberGenerator.GetBytes(bytes);
            return Base64Url(buf);
        }

        public static string Sha256Hex(string value)
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        public static string Base64Url(byte[] data) =>
            Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        // PKCE S256: BASE64URL(SHA256(verifier)) == challenge
        public static bool VerifyPkceS256(string? verifier, string? challenge)
        {
            if (string.IsNullOrEmpty(verifier) || string.IsNullOrEmpty(challenge)) return false;
            if (verifier.Length < 43 || verifier.Length > 128) return false;
            var computed = Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
            return CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(computed), Encoding.ASCII.GetBytes(challenge));
        }
    }
}
