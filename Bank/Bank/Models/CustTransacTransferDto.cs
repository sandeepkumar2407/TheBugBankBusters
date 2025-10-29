namespace Bank.Models
{
    public class CustTransacTransferDto
    {
        public int FromAcc { get; set; }
        public int ToAcc { get; set; }
        public decimal Amount { get; set; }
        public string? Comments { get; set; }
        public string? TransactionPass { get; set; }
    }
}