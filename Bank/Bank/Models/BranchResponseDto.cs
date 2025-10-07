namespace Bank.Models
{
    public class BranchResponseDto
    {
        public int BranchId { get; set; }

        public string ?BranchName { get; set; }

        public string? Baddress { get; set; }

        public string ?IfscCode { get; set; }
    }
}
