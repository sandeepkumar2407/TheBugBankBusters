using System.ComponentModel.DataAnnotations;

namespace Bank.Models
{
    public class BranchDto
    {
        //public int BranchId { get; set; }

        public string BranchName { get; set; } = null!;

        public string? Baddress { get; set; }

        public string IfscCode { get; set; } = null!;
    }
    public class BranchDtoGet
    {
        public int BranchId { get; set; }

        public string BranchName { get; set; } = null!;

        public string? Baddress { get; set; }

        public string IfscCode { get; set; } = null!;
    }

    public class BranchUpdateDto
    {
        public string? BranchName { get; set; } = null!;

        public string? Baddress { get; set; }

        public string? IfscCode { get; set; } = null!;
    }

}
