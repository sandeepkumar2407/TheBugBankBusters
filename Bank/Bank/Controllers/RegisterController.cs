using Bank.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Bank.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RegisterController : ControllerBase
    {
        readonly BankDbContext bankDbContext;

        public RegisterController(BankDbContext _bankDbContext)
        {
            this.bankDbContext = _bankDbContext;
        }

        private static string GenerateRandomPassword(int length)
        {
            const string validChars = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789@#$&";
            Random random = new Random();
            return new string(Enumerable.Repeat(validChars, length)
                                        .Select(s => s[random.Next(s.Length)])
                                        .ToArray());
        }
        [HttpPost("RegisterUser")]
        public IActionResult RegisterUser(UserCreationDto newUser)
        {
            try
            {
                if (newUser == null)
                {
                    return BadRequest("Invalid user data");
                }

                if (!string.IsNullOrWhiteSpace(newUser.Mobile) && bankDbContext.Users.Any(u => u.Mobile == newUser.Mobile && u.SoftDelete == true))
                {
                    return BadRequest("Mobile number already exists");
                }

                if (!string.IsNullOrWhiteSpace(newUser.Email) && bankDbContext.Users.Any(u => u.Email == newUser.Email && u.SoftDelete == true))
                {
                    return BadRequest("Email already exists");
                }

                if (!string.IsNullOrWhiteSpace(newUser.PANCard) && bankDbContext.Users.Any(u => u.PANCard == newUser.PANCard && u.SoftDelete == true))
                {
                    return BadRequest("PAN card already exists");
                }

                if (!string.IsNullOrWhiteSpace(newUser.AadharCard) && bankDbContext.Users.Any(u => u.AadharCard == newUser.AadharCard && u.SoftDelete == true))
                {
                    return BadRequest("Aadhar card already exists");
                }

                string generatedPassword = GenerateRandomPassword(12);

                User u = new User()
                {
                    Uname = newUser.UserName,
                    DoB = newUser.DoB,
                    Uaddress = newUser.UAddress,
                    Gender = newUser.Gender,
                    Mobile = newUser.Mobile,
                    Email = newUser.Email,
                    PANCard = newUser.PANCard,
                    AadharCard = newUser.AadharCard,
                    LoginPassword = generatedPassword,
                    TransactionPassword = generatedPassword,
                    SoftDelete = true,
                    Role = "Customer"
                };
                bankDbContext.Users.Add(u);
                bankDbContext.SaveChanges();

                var response = new
                {
                    message = "You have successfully registered with out bank! Here are your details, Please change your passwords.",
                    userDetails = new
                    {
                        UserId = u.UserId,
                        UserName = u.Uname,
                        Email = u.Email,
                        Mobile = u.Mobile,
                        LoginPassword = u.LoginPassword,
                        TransactionPassword = u.TransactionPassword
                    }
                };
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}