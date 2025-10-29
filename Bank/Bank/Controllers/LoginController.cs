using Bank.Models;
using Bank.Services;
using Microsoft.AspNetCore.Http;
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

        public LoginController(BankDbContext _context, IJwtServices _jwtServices)
        {
            context = _context;
            jwtServices = _jwtServices;
        }

        // POST: api/Login/Staff
        [HttpPost("Staff")]
        public IActionResult StaffLogin([FromBody] LoginDto login)
        {
            try
            {
                if (login == null || login.EmpId == null)
                {
                    return BadRequest(new { message = "Invalid login data" });
                }
                    
                var emp = context.Staff.Include(e=>e.Branch).FirstOrDefault(e => e.EmpId == login.EmpId);

                if (emp == null)
                {
                    return NotFound(new { message = "Employee not found" });
                }   

                if (emp.EmpPass != login.Password)
                {
                    return Unauthorized(new { message = "Invalid password" });
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

                return Ok(new LoginResponseDto
                {
                    Token = token,
                    Message = "Login successful",
                    Role = emp.EmpRole,
                    UserId = emp.EmpId,
                    BranchId = emp.BranchId,
                    BranchName = emp.Branch?.BranchName
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Server error", error = ex.Message });
            }
        }

        //POST: api/Login/Customer

        [HttpPost("Customer")]

        public IActionResult CustomerLogin([FromBody] LoginCusDto login)
        {
            try
            {
                if(login == null || login.UserId == null)
                {
                    return BadRequest(new { message = "Invalid login data" });
                } 
                var customer = context.Users.FirstOrDefault(u=>u.UserId == login.UserId);
                if(customer == null)
                {
                    return NotFound(new { message = "Customer not found" });
                }
                if(login.Password != customer.LoginPassword)
                {
                    return Unauthorized(new { message = "Invalid Password" });
                }
                if (!customer.SoftDelete)
                {
                    return NotFound(new { message = "Customer account is inactive" });
                }
                var token = jwtServices.GenerateToken(
                    role: "Customer",
                    userId: customer.UserId
                    );
                return Ok(new LoginResponseDto
                {
                    Token = token,
                    Message = "Login Successful",
                    Role = "Customer",
                    UserId = customer.UserId,
                    BranchId = null,
                    BranchName = null,
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
            return Ok(new { message = "Logged out successfully" });
        }
    }
}