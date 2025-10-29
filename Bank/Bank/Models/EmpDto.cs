namespace Bank.Models
{
    public class EmpDto
    {
        public string EmpName { get; set; } = null!;

        public string EmpRole { get; set; } = null!;

        public string EmpMobile { get; set; } = null!;

        public string EmpEmail { get; set; } = null!;

        public string EmpPass { get; set; } = null!;

        public int? BranchId { get; set; }
    }

    public class EmpUpdateDto
    {
        public string? EmpName { get; set; }

        public string? EmpRole { get; set; }

        public string? EmpMobile { get; set; }

        public string? EmpEmail { get; set; }

        public int? BranchId { get; set; }
    }

    public class EmpDtoGet
    {
        public int EmpId { get; set; }
        public string EmpName { get; set; } = null!;

        public string EmpRole { get; set; } = null!;

        public string EmpMobile { get; set; } = null!;

        public string EmpEmail { get; set; } = null!;

        public int? BranchId { get; set; }
    }
}
