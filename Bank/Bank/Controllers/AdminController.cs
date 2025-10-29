using Bank.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace Bank.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles="BankAdmin")]
    public class AdminController : ControllerBase
    {
        readonly BankDbContext bankDbContext;

        public AdminController(BankDbContext _bankDbContext)
        {
            this.bankDbContext = _bankDbContext;
        }

        ////////// BRANCH PART //////////

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

                var response = new
                {
                    message = "Branch Registered Successfully",
                    branchDetails = new
                    {
                        BranchId = br.BranchId,
                        BranchName = br.BranchName,
                        BranchAddress = br.Baddress,
                        IFSCcode = ifscCode
                    }
                };
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("GetBranchById/{id}")]
        public IActionResult GetBranchById(int id)
        {
            try
            {
                var branch = bankDbContext.Branches.FirstOrDefault(b => b.BranchId == id);

                if (branch == null)
                {
                    return NotFound($"No Branch found with ID: {id}");
                }
                var br = new BranchResponseDto()
                {
                    BranchId = branch.BranchId,
                    BranchName = branch.BranchName,
                    Baddress = branch.Baddress,
                    IfscCode = branch.IfscCode
                };

                return Ok(br);
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
                    
                if (!string.IsNullOrWhiteSpace(br.IfscCode))
                {
                    bool ifscExists = bankDbContext.Branches.Any(b => b.IfscCode == br.IfscCode && b.BranchId != id);
                    if (ifscExists)
                    {
                        return BadRequest("IFSC code already exists. Please use a unique one.");
                    }
                    res.IfscCode = br.IfscCode;
                }

                if (!string.IsNullOrWhiteSpace(br.BranchName))
                {
                    res.BranchName = br.BranchName;
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

        ////////// STAFF PART //////////

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

                var checkBranch = bankDbContext.Branches.Find(staffDto.BranchId);

                if (checkBranch == null)
                {
                    return BadRequest("Branch is not existing");
                }

                if (bankDbContext.Staff.Any(s => s.EmpEmail == staffDto.EmpEmail))
                {
                    return BadRequest("Email already exists. Please use a different email.");
                }

                if (bankDbContext.Staff.Any(s => s.EmpMobile == staffDto.EmpMobile))
                {
                    return BadRequest("Mobile number already exists. Please use a different number.");
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
                    SoftDelete = true
                };

                bankDbContext.Staff.Add(st);
                bankDbContext.SaveChanges();

                var response = new
                {
                    message = "Staff added successfully. Please change your password after first login.",
                    staffDetails = new
                    {
                        EmpId = st.EmpId,
                        EmpName = st.EmpName,
                        EmpEmail = st.EmpEmail,
                        EmpMobile = st.EmpMobile,
                        EmpRole = st.EmpRole,
                        BranchId = st.BranchId,
                        BranchName = checkBranch.BranchName,
                        GeneratedPassword = st.EmpPass
                    }
                };

                return Ok(response);
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
                if (id < 100000)
                {
                    return BadRequest("Provide valid id");
                }

                var employee = bankDbContext.Staff.Find(id);
                if (employee == null) 
                { 
                    return NotFound("Employee not found"); 
                }

                if (!string.IsNullOrWhiteSpace(staffUpdateDto.EmpEmail))
                {
                    bool emailExists = bankDbContext.Staff.Any(s => s.EmpEmail == staffUpdateDto.EmpEmail && s.EmpId != id);
                    if (emailExists)
                    {
                        return BadRequest("Email already exists. Please use a different email.");
                    }
                    employee.EmpEmail = staffUpdateDto.EmpEmail;
                }

                if (!string.IsNullOrWhiteSpace(staffUpdateDto.EmpMobile))
                {
                    bool mobileExists = bankDbContext.Staff.Any(s => s.EmpMobile == staffUpdateDto.EmpMobile && s.EmpId != id);
                    if (mobileExists)
                    {
                        return BadRequest("Mobile number already exists. Please use a different number.");
                    }
                    employee.EmpMobile = staffUpdateDto.EmpMobile;
                }

                if (!string.IsNullOrWhiteSpace(staffUpdateDto.EmpName))
                {
                    employee.EmpName = staffUpdateDto.EmpName;
                }
                    
                if (staffUpdateDto.BranchId.HasValue && staffUpdateDto.BranchId.Value > 0)
                {
                    var branch = bankDbContext.Branches.Find(staffUpdateDto.BranchId.Value);
                    if (branch == null)
                    {
                        return NotFound("Branch not found");
                    }
                    employee.BranchId = staffUpdateDto.BranchId.Value;
                }

                if (!string.IsNullOrWhiteSpace(staffUpdateDto.EmpRole))
                {
                    employee.EmpRole = staffUpdateDto.EmpRole;
                }
                    
                bankDbContext.SaveChanges();
                return Ok("Updated successfully");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPatch("RemoveStaff/{id}")]
        public IActionResult RemoveStaff([FromRoute] int id)
        {
            try
            {
                if (id < 100000)
                {
                    return BadRequest("Please give proper id");
                }

                var emp = bankDbContext.Staff.Find(id);
                if (emp == null)
                {
                    return NotFound("Staff data not found");
                }
                emp.SoftDelete = false;
                bankDbContext.SaveChanges();

                return Ok("Deleted successfully");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet("GetAllStaff")]
        public IActionResult GetAllStaff()
        {
            try
            {
                var staff = bankDbContext.Staff.ToList();
                if(staff == null)
                {
                    return BadRequest("Staff details cannot be null");
                }
                return Ok(staff);
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
                if(branchId <= 0)
                {
                    return BadRequest("Please provide valid Branch ID");
                }
                var staffList = bankDbContext.Staff
                    .Where(s => s.BranchId == branchId && s.SoftDelete == true)
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
                if (id < 100000)
                {
                    return BadRequest("Please provide valid Staff ID");
                }
                var emp = bankDbContext.Staff
                            .Where(s => s.EmpId == id && s.SoftDelete == true)
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

        [HttpGet("GetDeletedStaff")]
        public IActionResult GetDStaff()
        {
            try
            {
                var dstaff = bankDbContext.Staff
                    .Where(s => s.SoftDelete == false)
                    .ToList();
                if (dstaff.Count == 0)
                {
                    return NotFound("No deleted Staff");
                }
                return Ok(dstaff);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("PermanantDeleteStaff/{id}")]
        public IActionResult PostDStaff(int id)
        {
            try
            {
                if(id < 100000)
                {
                    return BadRequest("Please provide valid Staff ID");
                }
                var dstaff = bankDbContext.Staff
                    .Where(s => s.EmpId == id && s.SoftDelete == false)
                    .FirstOrDefault();

                if (dstaff == null)
                {
                    return NotFound("No Staff deleted with the given ID");
                }
                bankDbContext.Remove(dstaff);
                bankDbContext.SaveChanges();
                return Ok("Permanantly deleted the staff");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}