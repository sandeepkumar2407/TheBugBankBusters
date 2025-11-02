using Bank.Models;
using Bank.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bank.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RegisterController : ControllerBase
    {
        private readonly BankDbContext bankDbContext;
        private readonly PasswordService passwordService;

        public RegisterController(BankDbContext _bankDbContext,PasswordService _passwordService)
        {
            bankDbContext = _bankDbContext;
            passwordService = _passwordService;
        }

        //private static string GenerateRandomPassword(int length)
        //{
        //    const string validChars = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789@#$&";
        //    Random random = new Random();
        //    return new string(Enumerable.Repeat(validChars, length)
        //                                .Select(s => s[random.Next(s.Length)])
        //                                .ToArray());
        //}

        [HttpPost("RegisterUser")]
        public async Task<IActionResult> RegisterUser([FromBody] UserCreationDto newUser)
        {
            try
            {
                if (newUser == null)
                {
                    return BadRequest(new { message = "Invalid user data" });
                }

                if (!string.IsNullOrWhiteSpace(newUser.Mobile) &&
                    await bankDbContext.Users.AnyAsync(u => u.Mobile == newUser.Mobile && u.SoftDelete == true))
                {
                    return BadRequest(new { message = "Mobile number already exists" });
                }

                if (!string.IsNullOrWhiteSpace(newUser.Email) &&
                    await bankDbContext.Users.AnyAsync(u => u.Email == newUser.Email && u.SoftDelete == true))
                {
                    return BadRequest(new { message = "Email already exists" });
                }

                if (!string.IsNullOrWhiteSpace(newUser.PANCard) &&
                    await bankDbContext.Users.AnyAsync(u => u.PANCard == newUser.PANCard && u.SoftDelete == true))
                {
                    return BadRequest(new { message = "PAN card already exists" });
                }

                if (!string.IsNullOrWhiteSpace(newUser.AadharCard) &&
                    await bankDbContext.Users.AnyAsync(u => u.AadharCard == newUser.AadharCard && u.SoftDelete == true))
                {
                    return BadRequest(new { message = "Aadhar card already exists" });
                }

                string generatedPassword = $"{newUser.Mobile}@123";
                string hashedPassword = passwordService.HashPassword(generatedPassword);

                var user = new User
                {
                    Uname = newUser.UserName,
                    DoB = newUser.DoB,
                    Uaddress = newUser.UAddress,
                    Gender = newUser.Gender,
                    Mobile = newUser.Mobile,
                    Email = newUser.Email,
                    PANCard = newUser.PANCard,
                    AadharCard = newUser.AadharCard,
                    LoginPassword = hashedPassword,
                    TransactionPassword = hashedPassword,
                    SoftDelete = true,
                    Role = "Customer"
                };

                await bankDbContext.Users.AddAsync(user);
                await bankDbContext.SaveChangesAsync();

                var response = new
                {
                    message = "You have successfully registered with our bank! Here are your details. Please change your passwords.",
                    userDetails = new
                    {
                        UserId = user.UserId,
                        UserName = user.Uname,
                        Email = user.Email,
                        Mobile = user.Mobile,
                        LoginPassword = generatedPassword,
                        TransactionPassword = generatedPassword
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}