namespace Bank.Models
{
    public class ScheduleTransactionDto
    {
        public int FromAccountId { get; set; }
        public int ToAccountId { get; set; }
        public decimal Amount { get; set; }
        public DateTime ScheduledTime { get; set; }
        public string TransactionPass { get; set; } = null!;
        
    }
}
