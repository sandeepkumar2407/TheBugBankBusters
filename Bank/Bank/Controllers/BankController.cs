using Bank.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Bank.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BankController : ControllerBase
    {
        readonly BankDbContext bnk;
        public BankController(BankDbContext context)
        {
            bnk = context;
        }
        [HttpPost("BranchRegister")]
        public IActionResult BranchRegister(BranchDto b)
        {
            try
            {
                Branch br = new Branch()
                {
                    //BranchId = b.BranchId,
                    BranchName = b.BranchName,
                    Baddress = b.Baddress,
                    IfscCode = b.IfscCode
                };
                bnk.Branches.Add(br);
                bnk.SaveChanges();
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
                var data = bnk.Branches.ToList();
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
                var res = bnk.Branches.Find(id);
                if (res == null)
                {
                    return BadRequest("No data found");
                }
                
                if (!string.IsNullOrWhiteSpace(br.BranchName))
                {
                    res.BranchName = br.BranchName;
                }
                if (!string.IsNullOrWhiteSpace(br.IfscCode)) { 
                    res.IfscCode = br.IfscCode;
                }
                if (!string.IsNullOrWhiteSpace(br.Baddress))
                {
                    res.Baddress = br.Baddress;
                }
                bnk.SaveChanges();
                return Ok("Branch Data Updated Successfully");

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        //Employee Management
        [HttpPost("AddEmployee")]
        public IActionResult AddEmployee(EmpDto emp)
        {
            try
            {
                if (emp == null)
                {
                    return BadRequest("Data is missing");
                }
                var CheckBranch = bnk.Branches.Find(emp.BranchId);
                if (CheckBranch == null)
                {
                    return BadRequest("Branch is not existing");
                }
                Staff st = new Staff()
                {
                    EmpName = emp.EmpName,
                    EmpEmail = emp.EmpEmail,
                    EmpMobile = emp.EmpMobile,
                    EmpRole = emp.EmpRole,
                    EmpPass = emp.EmpPass,
                    BranchId = emp.BranchId,

                };
                bnk.Staff.Add(st);
                bnk.SaveChanges();
                return Ok($"Employee added successfully\n{st}");

            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPut("UpdateEmp/{id}")]
        public IActionResult UpdateEmp([FromRoute] int id, [FromBody] EmpUpdateDto emp) {
            try
            {
                if (id <= 0) return BadRequest("Provide valid id");

                var employee = bnk.Staff.Find(id);
                if (employee == null) return NotFound("Employee not found");

                // Update only provided fields
                if (!string.IsNullOrWhiteSpace(emp.EmpName))
                    employee.EmpName = emp.EmpName;

                if (!string.IsNullOrWhiteSpace(emp.EmpEmail))
                    employee.EmpEmail = emp.EmpEmail;

                if (!string.IsNullOrWhiteSpace(emp.EmpMobile))
                    employee.EmpMobile = emp.EmpMobile;

                if (emp.BranchId.HasValue && emp.BranchId.Value > 0)
                {
                    var branch = bnk.Branches.Find(emp.BranchId.Value);
                    if (branch == null)
                        return NotFound("Branch not found");
                    employee.BranchId = emp.BranchId.Value;
                }

                if (!string.IsNullOrWhiteSpace(emp.EmpRole))
                    employee.EmpRole = emp.EmpRole;

                bnk.SaveChanges();
                return Ok("Updated successfully");
            }
            catch (Exception ex) { 
                return BadRequest(ex.Message);
            }
        }
        [HttpDelete("RemoveEmp/{id}")]
        public IActionResult RemoveEmp([FromRoute] int id)
        {
            if(id <= 0)
            {
                return BadRequest("Heyyy Gaandu give proper id");
            }
            var emp =  bnk.Staff.Find(id);
            if (emp == null) return NotFound("Emp data not found");
            bnk.Staff.Remove(emp);
            bnk.SaveChanges();
            return Ok("Deleted successfully");
            
        }
    }
}
