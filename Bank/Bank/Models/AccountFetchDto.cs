namespace Bank.Models
{
    public class AccountFetchDto
    {
        public int AccNo { get; set; }

        public int UserId { get; set; }

        public string AccType { get; set; } = null!;

        public decimal Balance { get; set; }

        public DateTime DateOfJoining { get; set; }

        public string IfscCode { get; set; } = null!;

        public string AccountStatus { get; set; } = null!;
    }
}
