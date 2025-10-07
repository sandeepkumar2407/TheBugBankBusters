using Bank.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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

        // POST: api/Login
        [HttpPost]
        public IActionResult Login([FromBody] LoginDto login)
        {
            try
            {
                if (login == null)
                    return BadRequest(new { message = "Invalid login data" });

                var emp = _context.Staff.FirstOrDefault(e => e.EmpId == login.EmpId);
                if (emp == null)
                    return NotFound(new { message = "Employee not found" });

                if (emp.EmpPass != login.Password)
                    return Unauthorized(new { message = "Invalid password" });

                return Ok(new
                {
                    message = "Login successful",
                    role = emp.EmpRole,
                    empId = emp.EmpId,
                    branchId = emp.BranchId
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Server error", error = ex.Message });
            }
        }

        // POST: api/Login/Logout
        [HttpPost("Logout")]
        public IActionResult Logout()
        {
            return Ok("Logged out successfully");
        }
    }
}
