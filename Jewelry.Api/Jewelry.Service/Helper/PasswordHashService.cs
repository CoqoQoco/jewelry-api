using jewelry.Model.User.PasswordHash;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Jewelry.Service.Helper
{
    public interface IPasswordHash
    {
        PasswordHashPorvider CreateHash(string password);
        byte[] CreateHash(string password, byte[] salt);
        bool ValidatePassword(string password, byte[] salt, byte[] correctHash);
        bool CompareHashes(byte[] array1, byte[] array2);
    }
    public class PasswordHashService : IPasswordHash
    {
        /// <summary>
        /// The salt byte size, 64 length ensures safety but could be increased / decreased
        /// </summary>
        private const int SaltByteSize = 64;
        /// <summary>
        /// The hash byte size, 
        /// </summary>
        private const int HashByteSize = 64;
        /// <summary>
        ///  High iteration count is less likely to be cracked
        /// </summary>
        private const int Pbkdf2Iterations = 10000;

        /// <summary>
        /// Creates a salted PBKDF2 hash of the password.
        /// </summary>
        /// <remarks>
        /// The salt and the hash have to be persisted side by side for the password. They could be persisted as bytes or as a string using the convenience methods in the next class to convert from byte[] to string and later back again when executing password validation.
        /// </remarks>
        /// <param name="password">The password to hash.</param>
        /// <returns>The hash of the password.</returns>
        public PasswordHashPorvider CreateHash(string password)
        {
            // Generate a random salt
            using (var csprng = new RNGCryptoServiceProvider())
            {
                // create a unique salt for every password hash to prevent rainbow and dictionary based attacks
                var salt = new byte[SaltByteSize];
                csprng.GetBytes(salt);

                // Hash the password and encode the parameters
                var hash = Pbkdf2(password, salt, Pbkdf2Iterations, HashByteSize);

                return new PasswordHashPorvider(hash, salt);
            }
        }
        /// <summary>
        /// Recreates a password hash based on the incoming password string and the stored salt
        /// </summary>
        /// <param name="password">The password to check.</param>
        /// <param name="salt">The salt existing.</param>
        /// <returns>the generated hash based on the password and salt</returns>
        public byte[] CreateHash(string password, byte[] salt)
        {
            // Extract the parameters from the hash
            return Pbkdf2(password, salt, Pbkdf2Iterations, HashByteSize);
        }

        /// <summary>
        /// Validates a password given a hash of the correct one.
        /// </summary>
        /// <param name="password">The password to check.</param>
        /// <param name="salt">The existing stored salt.</param>
        /// <param name="correctHash">The hash of the existing password.</param>
        /// <returns><c>true</c> if the password is correct. <c>false</c> otherwise. </returns>
        public bool ValidatePassword(string password, byte[] salt, byte[] correctHash)
        {
            // Extract the parameters from the hash
            byte[] testHash = Pbkdf2(password, salt, Pbkdf2Iterations, HashByteSize);
            return CompareHashes(correctHash, testHash);
        }
        /// <summary>
        /// Compares two byte arrays (hashes)
        /// </summary>
        /// <param name="array1">The array1.</param>
        /// <param name="array2">The array2.</param>
        /// <returns><c>true</c> if they are the same, otherwise <c>false</c></returns>
        public bool CompareHashes(byte[] array1, byte[] array2)
        {
            if (array1.Length != array2.Length) return false;
            return !array1.Where((t, i) => t != array2[i]).Any();
        }

        /// <summary>
        /// Computes the PBKDF2-SHA1 hash of a password.
        /// </summary>
        /// <param name="password">The password to hash.</param>
        /// <param name="salt">The salt.</param>
        /// <param name="iterations">The PBKDF2 iteration count.</param>
        /// <param name="outputBytes">The length of the hash to generate, in bytes.</param>
        /// <returns>A hash of the password.</returns>
        private byte[] Pbkdf2(string password, byte[] salt, int iterations, int outputBytes)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt))
            {
                pbkdf2.IterationCount = iterations;
                return pbkdf2.GetBytes(outputBytes);
            }
        }
    }

}
