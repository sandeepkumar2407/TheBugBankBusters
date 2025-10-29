using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Bank.Controllers
{
    public class BaseController : ControllerBase
    {
        protected int? GetUserId()
        {
            var userIdClaim = User?.FindFirst("UserId");
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                return userId;
            return null;
        }

        protected int? GetEmpId()
        {
            var empIdClaim = User?.FindFirst("EmpId");
            if (empIdClaim != null && int.TryParse(empIdClaim.Value, out int empId))
                return empId;
            return null;
        }

        protected int? GetBranchId()
        {
            var branchIdClaim = User?.FindFirst("BranchId");
            if (branchIdClaim != null && int.TryParse(branchIdClaim.Value, out int branchId))
                return branchId;
            return null;
        }

        protected string? GetRole()
        {
            return User?.FindFirst(ClaimTypes.Role)?.Value;
        }
    }
}