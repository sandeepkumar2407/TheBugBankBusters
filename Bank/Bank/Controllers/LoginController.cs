using Bank.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Bank.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        private readonly BankDbContext bankDbContext;

        public LoginController(BankDbContext context)
        {
            bankDbContext = context;
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginDto login)
        {
            try
            {
                if (login == null)
                    return BadRequest("Invalid login data");

                var emp = bankDbContext.Staff.FirstOrDefault(e => e.EmpId == login.EmpId);
                if (emp == null)
                    return NotFound("Employee not found");

                if (emp.EmpPass != login.Password)
                    return Unauthorized("Invalid password");

                //Create Claims
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, emp.EmpId.ToString()),
                    new Claim(ClaimTypes.Role, emp.EmpRole)
                };

                if (emp.BranchId != null)
                    claims.Add(new Claim("BranchId", emp.BranchId.ToString()!));

                //Create Claims Identity
                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                //Sign In User
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity));

                return Ok(new
                {
                    Message = "Login successful",
                    Role = emp.EmpRole,
                    EmpId = emp.EmpId,
                    BranchId = emp.BranchId
                });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("Logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Ok("Logged out successfully");
        }

        [HttpGet("AccessDenied")]
        public IActionResult AccessDenied()
        {
            return Forbid("You do not have access to this resource.");
        }
    }
}