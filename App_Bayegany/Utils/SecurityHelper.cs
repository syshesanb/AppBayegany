using System;
using System.Security.Cryptography;
using System.Text;

namespace BayeganyApp.Utils
{
    public static class SecurityHelper
    {
        private const string Salt = "BayeganyApp_SecureSalt_1405_#$!";

        public static string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                password = "";

            using var sha256 = SHA256.Create();
            byte[] inputBytes = Encoding.UTF8.GetBytes(password + Salt);
            byte[] hashBytes = sha256.ComputeHash(inputBytes);
            return Convert.ToHexString(hashBytes);
        }

        public static bool VerifyPassword(string inputPassword, string storedHash)
        {
            if (string.IsNullOrEmpty(storedHash))
                return false;

            string computedHash = HashPassword(inputPassword);
            return string.Equals(computedHash, storedHash, StringComparison.OrdinalIgnoreCase);
        }
    }
}
