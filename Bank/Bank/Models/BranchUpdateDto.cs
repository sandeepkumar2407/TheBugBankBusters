namespace Bank.Models
{
    public class BranchUpdateDto
    {
        public string ?BranchName { get; set; } = null!;

        public string? Baddress { get; set; }

        public string ?IfscCode { get; set; } = null!;
    }
}
