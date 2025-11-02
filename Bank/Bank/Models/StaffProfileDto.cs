namespace Bank.Models
{
    public class StaffProfileDto
    {
        public int EmpId { get; set; }
        public string EmpName { get; set; } = null!;
        public string EmpRole { get; set; } = null!;
        public string EmpMobile { get; set; } = null!;
        public string EmpEmail { get; set; } = null!;
        public int? BranchId { get; set; } = null!;
        public string BranchName { get; set; } = null!;
        public string BranchAddress { get; set; } = null!;
    }
}