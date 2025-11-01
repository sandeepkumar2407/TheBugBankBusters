using Microsoft.AspNetCore.Identity;
using System.Globalization;

namespace Bank.Services
{
    public class PasswordService
    {
        private readonly PasswordHasher<object> passwordHasher;

        public PasswordService()
        {
            passwordHasher = new PasswordHasher<object>();
        }

        public string HashPassword(string plainpassword)
        {
            return passwordHasher.HashPassword(new object(), plainpassword);
        }

        public bool VerifyPassword(string hashedPassword,string plainPassword)
        {
            var result = passwordHasher.VerifyHashedPassword(new object(), hashedPassword, plainPassword);
            return result == PasswordVerificationResult.Success;
        } 
    }
}