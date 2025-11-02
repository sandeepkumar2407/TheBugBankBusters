using Bank.Models;
using Bank.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Bank.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles="BankAdmin")]
    public class AdminController : BaseController
    {
        readonly BankDbContext bankDbContext;
        readonly PasswordService passwordService;

        public AdminController(BankDbContext _bankDbContext,PasswordService _passwordService)
        {
            this.bankDbContext = _bankDbContext;
            this.passwordService = _passwordService;
        }

        [HttpGet("GetProfile")]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                var staffId = GetUserId(); 
                if (staffId == null)
                    return BadRequest(new { message = "Invalid staff ID" });

                var staff = await bankDbContext.Staff
                    .Include(s => s.Branch)
                    .Where(s => s.EmpId == staffId)
                    .Select(s => new StaffProfileDto
                    {
                        EmpId = s.EmpId,
                        EmpName = s.EmpName,
                        EmpRole = s.EmpRole,
                        EmpMobile = s.EmpMobile,
                        EmpEmail = s.EmpEmail,
                        BranchId = s.BranchId,
                        BranchName = s.Branch != null ? s.Branch.BranchName : string.Empty,
                        BranchAddress = s.Branch != null ? s.Branch.Baddress : string.Empty
                    })
                    .FirstOrDefaultAsync();

                if (staff == null)
                    return NotFound(new { message = $"Staff with ID {staffId} not found" });

                return Ok(staff);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }


        [HttpPatch("UpdateLoginPassword")]
        public async Task<IActionResult> UpdateLoginPassword([FromBody] UpdatePassDto passDto)
        {
            try
            {
                var EmpId = GetEmpId();
                if (passDto == null)
                    return BadRequest(new { message = "Invalid request" });

                var staff = await bankDbContext.Staff.FindAsync(EmpId);
                if (staff == null)
                    return NotFound(new { message = "Staff not found for this staff ID" });

                bool isPrevPass = passwordService.VerifyPassword(staff.EmpPass, passDto.previousPassword);
                if (!isPrevPass)
                    return BadRequest(new { message = "Your old password is incorrect" });

                if (passDto.newPassword != passDto.confirmPassword)
                    return BadRequest(new { message = "New password and confirmation do not match" });

                bool isNewPass = passwordService.VerifyPassword(staff.EmpPass, passDto.newPassword);
                if (isNewPass)
                    return BadRequest(new { message = "New password cannot be the same as the old password" });

                staff.EmpPass = passwordService.HashPassword(passDto.newPassword);
                await bankDbContext.SaveChangesAsync();

                return Ok(new { message = "Password updated successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = $"Error updating password: {ex.Message}" });
            }
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
        public async Task<IActionResult> BranchRegister([FromBody] BranchDto b)
        {
            try
            {
                if (b == null)
                    return BadRequest(new { message = "Enter correct branch details" });

                string ifscCode = GenerateUniqueIFSC();

                var branch = new Branch
                {
                    BranchName = b.BranchName,
                    Baddress = b.Baddress,
                    IfscCode = ifscCode
                };

                await bankDbContext.Branches.AddAsync(branch);
                await bankDbContext.SaveChangesAsync();

                return Ok(new
                {
                    message = "Branch registered successfully",
                    branchDetails = new
                    {
                        BranchId = branch.BranchId,
                        BranchName = branch.BranchName,
                        BranchAddress = branch.Baddress,
                        IFSCcode = ifscCode
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("GetBranchById/{id}")]
        public async Task<IActionResult> GetBranchById(int id)
        {
            try
            {
                var branch = await bankDbContext.Branches
                    .FirstOrDefaultAsync(b => b.BranchId == id);

                if (branch == null)
                    return NotFound(new { message = $"No branch found with ID {id}" });

                var response = new BranchResponseDto
                {
                    BranchId = branch.BranchId,
                    BranchName = branch.BranchName,
                    Baddress = branch.Baddress,
                    IfscCode = branch.IfscCode
                };

                return Ok(new
                {
                    message = "Branch details retrieved successfully",
                    branch = response
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }


        [HttpGet("GetAllBranches")]
        public async Task<IActionResult> GetAllBranches()
        {
            try
            {
                var branches = await bankDbContext.Branches
                    .Select(b => new BranchResponseDto
                    {
                        BranchId = b.BranchId,
                        BranchName = b.BranchName,
                        Baddress = b.Baddress,
                        IfscCode = b.IfscCode
                    })
                    .ToListAsync();

                if (branches.Count == 0)
                    return NotFound(new { message = "No branches found" });

                return Ok(new
                {
                    message = "All branches retrieved successfully",
                    branches = branches
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("UpdateBranch/{id}")]
        public async Task<IActionResult> UpdateBranch([FromRoute] int id, [FromBody] BranchUpdateDto br)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new { message = "Provide a valid branch ID" });

                var branch = await bankDbContext.Branches.FindAsync(id);

                if (branch == null)
                    return NotFound(new { message = "Branch not found" });

                if (!string.IsNullOrWhiteSpace(br.BranchName))
                    branch.BranchName = br.BranchName;

                if (!string.IsNullOrWhiteSpace(br.Baddress))
                    branch.Baddress = br.Baddress;

                await bankDbContext.SaveChangesAsync();

                return Ok(new { message = "Branch data updated successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }


        [HttpPost("AddStaff")]
        public async Task<IActionResult> AddStaff([FromBody] StaffDto staffDto)
        {
            try
            {
                if (staffDto == null)
                    return BadRequest(new { message = "Data is missing" });

                var checkBranch = await bankDbContext.Branches.FindAsync(staffDto.BranchId);
                if (checkBranch == null)
                    return BadRequest(new { message = "Branch is not existing" });

                if (await bankDbContext.Staff.AnyAsync(s => s.EmpEmail == staffDto.EmpEmail))
                    return BadRequest(new { message = "Email already exists. Please use a different email." });

                if (await bankDbContext.Staff.AnyAsync(s => s.EmpMobile == staffDto.EmpMobile))
                    return BadRequest(new { message = "Mobile number already exists. Please use a different number." });

                string generatedPassword = $"{staffDto.EmpMobile}@123";
                string hashedPassword = passwordService.HashPassword(generatedPassword);
                var st = new Staff
                {
                    EmpName = staffDto.EmpName,
                    EmpEmail = staffDto.EmpEmail,
                    EmpMobile = staffDto.EmpMobile,
                    EmpRole = staffDto.EmpRole,
                    EmpPass = hashedPassword,
                    BranchId = staffDto.BranchId,
                    SoftDelete = true
                };

                await bankDbContext.Staff.AddAsync(st);
                await bankDbContext.SaveChangesAsync();

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
                        GeneratedPassword = generatedPassword
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }


        [HttpPut("UpdateStaff/{id}")]
        public async Task<IActionResult> UpdateStaff([FromRoute] int id, [FromBody] StaffUpdateDto staffUpdateDto)
        {
            try
            {
                if (id < 100000)
                    return BadRequest(new { message = "Provide valid id" });

                var employee = await bankDbContext.Staff.FindAsync(id);
                if (employee == null)
                    return NotFound(new { message = "Employee not found" });

                if (!string.IsNullOrWhiteSpace(staffUpdateDto.EmpEmail))
                {
                    bool emailExists = await bankDbContext.Staff.AnyAsync(s => s.EmpEmail == staffUpdateDto.EmpEmail && s.EmpId != id);
                    if (emailExists)
                        return BadRequest(new { message = "Email already exists. Please use a different email." });
                    employee.EmpEmail = staffUpdateDto.EmpEmail;
                }

                if (!string.IsNullOrWhiteSpace(staffUpdateDto.EmpMobile))
                {
                    bool mobileExists = await bankDbContext.Staff.AnyAsync(s => s.EmpMobile == staffUpdateDto.EmpMobile && s.EmpId != id);
                    if (mobileExists)
                        return BadRequest(new { message = "Mobile number already exists. Please use a different number." });
                    employee.EmpMobile = staffUpdateDto.EmpMobile;
                }

                if (!string.IsNullOrWhiteSpace(staffUpdateDto.EmpName))
                    employee.EmpName = staffUpdateDto.EmpName;

                if (staffUpdateDto.BranchId.HasValue && staffUpdateDto.BranchId.Value > 0)
                {
                    var branch = await bankDbContext.Branches.FindAsync(staffUpdateDto.BranchId.Value);
                    if (branch == null)
                        return NotFound(new { message = "Branch not found" });

                    employee.BranchId = staffUpdateDto.BranchId.Value;
                }

                if (!string.IsNullOrWhiteSpace(staffUpdateDto.EmpRole))
                    employee.EmpRole = staffUpdateDto.EmpRole;

                await bankDbContext.SaveChangesAsync();

                return Ok(new { message = "Staff updated successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }


        [HttpPatch("RemoveStaff/{id}")]
        public async Task<IActionResult> RemoveStaff([FromRoute] int id)
        {
            try
            {
                if (id < 100000)
                    return BadRequest(new { message = "Please give proper id" });

                var emp = await bankDbContext.Staff.FindAsync(id);
                if (emp == null)
                    return NotFound(new { message = "Staff data not found" });

                emp.SoftDelete = false;
                await bankDbContext.SaveChangesAsync();

                return Ok(new { message = "Staff soft-deleted successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }


        [HttpGet("GetAllStaff")]
        public async Task<IActionResult> GetAllStaff()
        {
            try
            {
                var staff = await bankDbContext.Staff
                    .Where(s => s.SoftDelete==true)
                    .Select(s => new StaffResponseDto
                    {
                        EmpID = s.EmpId,
                        EmpName = s.EmpName,
                        EmpRole = s.EmpRole,
                        EmpMobile = s.EmpMobile,
                        EmpEmail = s.EmpEmail,
                        BranchId = s.BranchId
                    })
                    .ToListAsync();

                if (staff == null || staff.Count == 0)
                    return NotFound(new { message = "No staff found" });

                return Ok(new
                {
                    message = "Staff list retrieved successfully",
                    data = staff
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }


        [HttpGet("GetStaffByBranchId/{branchId}")]
        public async Task<IActionResult> GetStaffByBranchId(int branchId)
        {
            try
            {
                if (branchId <= 0)
                    return BadRequest(new { message = "Please provide valid Branch ID" });

                var staffList = await bankDbContext.Staff
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
                    .ToListAsync();

                if (staffList.Count == 0)
                    return NotFound(new { message = $"No staff found for branch ID {branchId}" });

                return Ok(new { message = "Staff list retrieved successfully", data = staffList });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }


        [HttpGet("GetStaffByID/{id}")]
        public async Task<IActionResult> GetStaffByID(int id)
        {
            try
            {
                if (id < 100000)
                    return BadRequest(new { message = "Please provide valid Staff ID" });

                var emp = await bankDbContext.Staff
                    .Where(s => s.EmpId == id && s.SoftDelete == true)
                    .Select(s => new StaffResponseDto
                    {
                        EmpID = s.EmpId,
                        EmpName = s.EmpName,
                        EmpRole = s.EmpRole,
                        EmpMobile = s.EmpMobile,
                        EmpEmail = s.EmpEmail,
                        BranchId = s.BranchId
                    })
                    .FirstOrDefaultAsync();

                if (emp == null)
                    return NotFound(new { message = $"Staff with StaffID {id} not found" });

                return Ok(new { message = "Staff retrieved successfully", data = emp });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }


        [HttpGet("GetDeletedStaff")]
        public async Task<IActionResult> GetDStaff()
        {
            try
            {
                var dstaff = await bankDbContext.Staff
                    .Where(s => s.SoftDelete == false)
                    .ToListAsync();

                if (dstaff.Count == 0)
                    return NotFound(new { message = "No deleted Staff" });

                return Ok(new { message = "Deleted staff retrieved successfully", data = dstaff });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }


        [HttpDelete("PermanantDeleteStaff/{id}")]
        public async Task<IActionResult> PostDStaff(int id)
        {
            try
            {
                if (id < 100000)
                    return BadRequest(new { message = "Please provide valid Staff ID" });

                var dstaff = await bankDbContext.Staff
                    .Where(s => s.EmpId == id && s.SoftDelete == false)
                    .FirstOrDefaultAsync();

                if (dstaff == null)
                    return NotFound(new { message = "No Staff deleted with the given ID" });

                bankDbContext.Remove(dstaff);
                await bankDbContext.SaveChangesAsync();

                return Ok(new { message = "Permanently deleted the staff" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /////INTEREST PART/////
        [HttpPut("ApplyFDInterest")]
        public async Task<IActionResult> ApplyFDInterest()
        {
            try
            {

                int[] interestRates = {5,6,7,8,9};
                Random random = new Random();
                int selectedRate = interestRates[random.Next(interestRates.Length)];

                var fdAccounts = await bankDbContext.Accounts
                    .Where(a => a.AccType == "Fixed Deposit")
                    .ToListAsync();

                if (fdAccounts.Count()==0)
                {
                    return NotFound(new { message = "No Fixed Deposit accounts found." });
                }

                foreach (var account in fdAccounts)
                {
                    decimal interest = account.Balance * selectedRate / 100;
                    account.Balance += interest;
                }

                await bankDbContext.SaveChangesAsync();

                return Ok(new
                {
                    message = $"Applied {selectedRate}% interest to all Fixed Deposit accounts.",
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("ApplySavingsInterest")]
        public async Task<IActionResult> ApplySavingsInterest()
        {
            try
            {

                int[] interestRates = {2,3,4};
                Random random = new Random();
                int selectedRate = interestRates[random.Next(interestRates.Length)];

                var fdAccounts = await bankDbContext.Accounts
                    .Where(a => a.AccType == "Savings")
                    .ToListAsync();

                if (fdAccounts.Count() == 0)
                {
                    return NotFound(new { message = "No Savings accounts found." });
                }

                foreach (var account in fdAccounts)
                {
                    decimal interest = account.Balance * selectedRate / 100;
                    account.Balance += interest;
                }

                await bankDbContext.SaveChangesAsync();

                return Ok(new
                {
                    message = $"Applied {selectedRate}% interest to all Savings accounts.",
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}