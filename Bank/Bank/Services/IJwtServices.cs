namespace Bank.Services
{
    public interface IJwtServices
    {
        string GenerateToken(string role, int? userId = null, int? empId = null, int? branchId = null);
    }
}
