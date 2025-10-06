namespace Bank.Models
{
    public class StaffDto
    {
        public int EmpID { get; set; }
        public string EmpName { get; set; } = null!;

        public string EmpRole { get; set; } = null!;

        public string EmpMobile { get; set; } = null!;

        public string EmpEmail { get; set; } = null!;

        //public string EmpPass { get; set; } = null!;

        public int? BranchId { get; set; }
    }
}
