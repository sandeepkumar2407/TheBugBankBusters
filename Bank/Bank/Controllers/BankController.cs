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
    }
}
