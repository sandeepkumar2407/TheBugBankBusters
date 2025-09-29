using Bank.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Bank.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class Manager2Controller : ControllerBase
    {
        readonly BankDbContext bnk;
        public Manager2Controller(BankDbContext context)
        {
            bnk = context;
        }

        [HttpGet("GetAllEmployees")]
        public IActionResult GetAllEmployees()
        {
            try
            {
                var empDtos = bnk.Staff
                .Select(e => new EmpDtoGet
                {
                    EmpId = e.EmpId,
                    EmpName = e.EmpName,
                    EmpRole = e.EmpRole,
                    EmpMobile = e.EmpMobile,
                    EmpEmail = e.EmpEmail,
                    BranchId = e.BranchId
                })
                .ToList();
                if (!empDtos.Any())
                {
                    return NotFound("No Employees Found");
                }
                return Ok(empDtos);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPost("AddCustomer")]
        public IActionResult AddCustomer(UserDto u)
        {
            try
            {
                if (u == null)
                {
                    return BadRequest("Invalid User Data");
                }
                User us = new User()
                {
                    Uname = u.Uname,
                    Uaddress = u.Uaddress,
                    DoB = u.DoB,
                    Gender = u.Gender,
                    Email = u.Email,
                    Mobile = u.Mobile
                };
                bnk.Users.Add(us);
                bnk.SaveChanges();
                return Ok("Customer Added Successfully");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet("GetAllCustomers")]
        public IActionResult GetAllCustomers()
        {
            try
            {
                var userDtos = bnk.Users
                .Select(u => new UserGetDto
                {
                    UserId = u.UserId,
                    Uname = u.Uname,
                    Uaddress = u.Uaddress,
                    DoB = u.DoB,
                    Gender = u.Gender,
                    Email = u.Email,
                    Mobile = u.Mobile
                }).ToList();
                if (!userDtos.Any())
                {
                    return NotFound("No Customers Found");
                }
                return Ok(userDtos);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPut("UpdateCustomer/{id}")]
        public IActionResult UpdateCustomer([FromRoute] int id, [FromBody] UserUpdateDto u)
        {
            try
            {
                if (id < 100) return BadRequest("Provide valid id");
                var user = bnk.Users.Find(id);
                if (user == null)
                {
                    return NotFound("Customer Not Found");
                }
                if (!string.IsNullOrWhiteSpace(u.Uname))
                {
                    user.Uname = u.Uname;
                }
                if (!string.IsNullOrWhiteSpace(u.Uaddress))
                {
                    user.Uaddress = u.Uaddress;
                }
                if (!string.IsNullOrWhiteSpace(u.Email)) 
                { 
                    user.Email = u.Email;
                }
                if (!string.IsNullOrWhiteSpace(u.Mobile))
                {
                    user.Mobile = u.Mobile;
                }
                bnk.SaveChanges();
                return Ok("Customer Updated Successfully");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpDelete("DeleteCustomer/{id}")]
        public IActionResult DeleteCustomer([FromRoute] int id)
        {
            try
            {
                if (id < 100) return BadRequest("Provide valid id");
                var user = bnk.Users.Find(id);
                if (user == null)
                {
                    return NotFound("Customer Not Found");
                }
                bnk.Users.Remove(user);
                bnk.SaveChanges();
                return Ok("Customer Deleted Successfully");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPost("CreateAccount")]
        public IActionResult CreateAccount(AccountDto a)
        {
            try
            {
                if (a == null)
                {
                    return BadRequest("Invalid Account Data");
                }
                var user = bnk.Users.Find(a.UserId);

                if (user == null)
                {
                    return NotFound("Customer Not Found");
                }
                var branch = bnk.Branches.Find(a.BranchId);
                if (branch == null)
                {
                    return NotFound("Branch Not Found");
                }
                Account acc = new Account()
                {
                    UserId = a.UserId,
                    AccType = a.AccType,
                    Balance = a.Balance,
                    DateOfJoining = DateTime.UtcNow,
                    IfscCode = branch.IfscCode
                };
                bnk.Accounts.Add(acc);
                bnk.SaveChanges();
                return Ok("Account Created Successfully");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
