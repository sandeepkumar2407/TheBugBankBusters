namespace Bank.Models
{
    public class TransactionCreditDebitDto
    {
        public int? AccNo { get; set; }
        public decimal Amount { get; set; }
        public string TransacType { get; set; } = null!;
        public string? Comments { get; set; }
    }
}
