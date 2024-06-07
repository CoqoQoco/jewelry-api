using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jewelry.Model.User.PasswordHash
{
    public class PasswordHashPorvider
    {
        /// <summary>
        /// Gets the hashed password.
        /// </summary>
        public byte[] HashedPassword { get; private set; }
        /// <summary>
        /// Gets the salt.
        /// </summary>
        public byte[] Salt { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PasswordHashContainer" /> class.
        /// </summary>
        /// <param name="hashedPassword">The hashed password.</param>
        /// <param name="salt">The salt.</param>
        public PasswordHashPorvider(byte[] hashedPassword, byte[] salt)
        {
            this.HashedPassword = hashedPassword;
            this.Salt = salt;
        }
    }
}
