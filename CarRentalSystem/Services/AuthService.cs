using System;
using System.Security.Cryptography;
using System.Text;

namespace CarRentalSystem.Services
{
    public static class AuthService
    {
        public static string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password)) return string.Empty;

            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.Unicode.GetBytes(password);
                byte[] hashBytes = sha256.ComputeHash(bytes);

                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    builder.Append(hashBytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}
