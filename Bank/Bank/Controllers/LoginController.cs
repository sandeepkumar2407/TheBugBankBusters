using Bank.Models;
using Bank.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bank.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        private readonly BankDbContext context;
        private readonly IJwtServices jwtServices;
        private readonly PasswordService passwordService;

        public LoginController(BankDbContext _context, IJwtServices _jwtServices, PasswordService _passwordService)
        {
            context = _context;
            jwtServices = _jwtServices;
            passwordService = _passwordService;
        }

        // POST: api/Login/Staff
        [HttpPost("Staff")]
        public async Task<IActionResult> StaffLogin([FromBody] LoginDto login)
        {
            try
            {
                if (login == null || login.EmpId == null)
                {
                    return BadRequest(new { message = "Invalid login data" });
                }

                var emp = await context.Staff
                    .Include(e => e.Branch)
                    .FirstOrDefaultAsync(e => e.EmpId == login.EmpId);

                if (emp == null)
                {
                    return NotFound(new { message = "Employee not found" });
                }

                bool isPassValid = passwordService.VerifyPassword(emp.EmpPass, login.Password);

                if (!isPassValid)
                {
                    return BadRequest(new { message = "Invalid password" });
                }

                if (!emp.SoftDelete)
                {
                    return NotFound(new { message = "Employee account is inactive" });
                }

                var token = jwtServices.GenerateToken(
                    role: emp.EmpRole,
                    empId: emp.EmpId,
                    branchId: emp.BranchId
                );

                var response = new
                {
                    Token = token,
                    Message = "Login successful",
                    Role = emp.EmpRole,
                    UserId = emp.EmpId,
                    BranchId = emp.BranchId,
                    BranchName = emp.Branch?.BranchName
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Server error", error = ex.Message });
            }
        }

        // POST: api/Login/Customer
        [HttpPost("Customer")]
        public async Task<IActionResult> CustomerLogin([FromBody] LoginCusDto login)
        {
            try
            {
                if (login == null || login.UserId == null)
                {
                    return BadRequest(new { message = "Invalid login data" });
                }

                var customer = await context.Users
                    .FirstOrDefaultAsync(u => u.UserId == login.UserId);

                if (customer == null)
                {
                    return NotFound(new { message = "Customer not found" });
                }

                bool isPassValid = passwordService.VerifyPassword(customer.LoginPassword, login.Password);

                if (!isPassValid)
                {
                    return BadRequest(new { message = "Invalid password" });
                }

                if (!customer.SoftDelete)
                {
                    return NotFound(new { message = "Customer account is inactive" });
                }

                var token = jwtServices.GenerateToken(
                    role: "Customer",
                    userId: customer.UserId
                );

                var response = new
                {
                    Token = token,
                    Message = "Login successful",
                    Role = "Customer",
                    UserId = customer.UserId,
                    BranchId = (int?)null,
                    BranchName = (string?)null
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Server error", error = ex.Message });
            }
        }

        // POST: api/Login/Logout
        [HttpPost("Logout")]
        public async Task<IActionResult> Logout()
        {
            await Task.CompletedTask; // placeholder for async consistency
            return Ok(new { message = "Logged out successfully" });
        }
    }
}