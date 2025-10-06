using Bank.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace Bank.Controllers
{
    [Authorize(Roles = "BankAdmin")]
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        readonly BankDbContext bankDbContext;

        public AdminController(BankDbContext _bankDbContext)
        {
            this.bankDbContext = _bankDbContext;
        }




        //////BRANCH PART///////



        private string GenerateUniqueIFSC()
        {
            const string prefix = "BUGB0000";
            Random random = new Random();
            string ifscCode;

            do
            {
                int num = random.Next(1000, 10000);
                ifscCode = prefix + num.ToString();
            }
            while (bankDbContext.Branches.Any(b => b.IfscCode == ifscCode));

            return ifscCode;
        }

        [HttpPost("BranchRegister")]
        public IActionResult BranchRegister(BranchDto b)
        {
            try
            {
                if (b == null)
                {
                    return BadRequest("Enter correct details");
                }

                string ifscCode = GenerateUniqueIFSC();

                Branch br = new Branch()
                {
                    BranchName = b.BranchName,
                    Baddress = b.Baddress,
                    IfscCode = ifscCode
                };
                bankDbContext.Branches.Add(br);
                bankDbContext.SaveChanges();
                return Ok("Branch Registered Successfully");
            }

            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("GetAllBranches")]
        public IActionResult GetAllBranches()
        {
            try
            {
                var data = bankDbContext.Branches.ToList();
                if (data.Count == 0)
                {
                    return NotFound("No Branches Found");
                }
                return Ok(data);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("UpdateBranch/{id}")]
        public IActionResult UpdateBranch([FromRoute] int id, [FromBody] BranchUpdateDto br)
        {
            try
            {
                if (id <= 0)
                {
                    return NotFound("Provide valid id");
                }
                var res = bankDbContext.Branches.Find(id);
                if (res == null)
                {
                    return BadRequest("No data found");
                }

                if (!string.IsNullOrWhiteSpace(br.BranchName))
                {
                    res.BranchName = br.BranchName;
                }
                if (!string.IsNullOrWhiteSpace(br.IfscCode))
                {
                    res.IfscCode = br.IfscCode;
                }
                if (!string.IsNullOrWhiteSpace(br.Baddress))
                {
                    res.Baddress = br.Baddress;
                }
                bankDbContext.SaveChanges();
                return Ok("Branch Data Updated Successfully");

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }





        //////STAFF PART///////


        private static string GenerateRandomPassword(int length)
        {
            const string validChars = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789@#$&";
            Random random = new Random();
            return new string(Enumerable.Repeat(validChars, length)
                                        .Select(s => s[random.Next(s.Length)])
                                        .ToArray());
        }

        [HttpPost("AddStaff")]
        public IActionResult AddStaff(StaffDto staffDto)
        {
            try
            {
                if (staffDto == null)
                {
                    return BadRequest("Data is missing");
                }

                var CheckBranch = bankDbContext.Branches.Find(staffDto.BranchId);

                if (CheckBranch == null)
                {
                    return BadRequest("Branch is not existing");
                }

                string generatedPassword = GenerateRandomPassword(12);

                Staff st = new Staff()
                {
                    EmpName = staffDto.EmpName,
                    EmpEmail = staffDto.EmpEmail,
                    EmpMobile = staffDto.EmpMobile,
                    EmpRole = staffDto.EmpRole,
                    EmpPass = generatedPassword,
                    BranchId = staffDto.BranchId,
                };
                bankDbContext.Staff.Add(st);
                bankDbContext.SaveChanges();
                return Ok($"Staff added successfully\n{st}");

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        
        [HttpPut("UpdateStaff/{id}")]
        public IActionResult UpdateStaff([FromRoute] int id, [FromBody] StaffUpdateDto staffUpdateDto)
        {
            try
            {
                if (id <= 0) return BadRequest("Provide valid id");

                var employee = bankDbContext.Staff.Find(id);
                if (employee == null) return NotFound("Employee not found");

                // Update only provided fields
                if (!string.IsNullOrWhiteSpace(staffUpdateDto.EmpName))
                    employee.EmpName = staffUpdateDto.EmpName;

                if (!string.IsNullOrWhiteSpace(staffUpdateDto.EmpEmail))
                    employee.EmpEmail = staffUpdateDto.EmpEmail;

                if (!string.IsNullOrWhiteSpace(staffUpdateDto.EmpMobile))
                    employee.EmpMobile = staffUpdateDto.EmpMobile;

                if (staffUpdateDto.BranchId.HasValue && staffUpdateDto.BranchId.Value > 0)
                {
                    var branch = bankDbContext.Branches.Find(staffUpdateDto.BranchId.Value);
                    if (branch == null)
                        return NotFound("Branch not found");
                    employee.BranchId = staffUpdateDto.BranchId.Value;
                }

                if (!string.IsNullOrWhiteSpace(staffUpdateDto.EmpRole))
                    employee.EmpRole = staffUpdateDto.EmpRole;

                bankDbContext.SaveChanges();
                return Ok("Updated successfully");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("RemoveStaff/{id}")]
        public IActionResult RemoveStaff([FromRoute] int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest("Please give proper id");
                }

                var emp = bankDbContext.Staff.Find(id);
                if (emp == null) return NotFound("Staff data not found");

                bankDbContext.Staff.Remove(emp);
                bankDbContext.SaveChanges();

                return Ok("Deleted successfully");
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }

        [HttpGet("GetStaffByBranchId/{branchId}")]
        public IActionResult GetStaffByBranchId(int branchId)
        {
            try
            {
                var staffList = bankDbContext.Staff
                    .Where(s => s.BranchId == branchId)
                    .Select(s => new StaffResponseDto
                    {
                        EmpID = s.EmpId,
                        EmpName = s.EmpName,
                        EmpRole = s.EmpRole,
                        EmpMobile = s.EmpMobile,
                        EmpEmail = s.EmpEmail,
                        BranchId = branchId
                    })
                    .ToList();

                if (staffList.Count == 0)
                {
                    return NotFound($"No staff found for branch ID {branchId}");
                }

                return Ok(staffList);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpGet("GetStaffByID/{id}")]
        public IActionResult GetStaffByID(int id)
        {
            try
            {
                var emp = bankDbContext.Staff
                            .Where(s => s.EmpId == id)
                            .Select(s => new StaffResponseDto
                            {
                                EmpID = s.EmpId,
                                EmpName = s.EmpName,
                                EmpRole = s.EmpRole,
                                EmpMobile = s.EmpMobile,
                                EmpEmail = s.EmpEmail,
                                BranchId = s.BranchId
                            }).FirstOrDefault();
                if (emp == null)
                {
                    return NotFound($"Staff with StaffID {id} not found");
                }
                return Ok(emp);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
