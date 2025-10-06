namespace Bank.Models
{
    public class AccountResponseDto
    {
        public int AccNo { get; set; }
        public string AccType { get; set; } = null!;
        public decimal Balance { get; set; }
        public DateTime DateOfJoining { get; set; }
        public string IfscCode { get; set; } = null!;
        public string AccountStatus { get; set; } = null!;


        public int UserId { get; set; }
        public string UserName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Mobile { get; set; } = null!;


        public int BranchId { get; set; }
        public string BranchName { get; set; } = null!;
        public string? BranchAddress { get; set; }
    }

}
