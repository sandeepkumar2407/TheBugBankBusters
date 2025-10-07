using Bank.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Bank.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        private readonly BankDbContext _context;

        public LoginController(BankDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginDto login)
        {
            try
            {
                if (login == null)
                    return BadRequest("Invalid login data");

                var emp = _context.Staff.FirstOrDefault(e => e.EmpId == login.EmpId);
                if (emp == null)
                    return NotFound("Employee not found");

                if (emp.EmpPass != login.Password)
                    return Unauthorized("Invalid password");

                // ✅ Create claims
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, emp.EmpId.ToString()),
                    new Claim(ClaimTypes.Role, emp.EmpRole)
                };

                if (emp.BranchId != null)
                    claims.Add(new Claim("BranchId", emp.BranchId.ToString()!));

                // ✅ Create identity
                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                // ✅ Set authentication cookie
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTime.UtcNow.AddHours(2),
                    AllowRefresh = true
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties
                );

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
