namespace Bank.Models
{
    public class CustomerMiniStatementDto
    {
        public int AccNo { get; set; }
        public string AccType { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public string AccountStatus { get; set; } = string.Empty;
        public DateTime DateOfJoining { get; set; }
        public string IFSC_Code { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string UName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public List<TransactionResponseDto> RecentTransactions { get; set; } = new List<TransactionResponseDto>();
    }
}
