using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bank.Models
{
    [Table("ScheduledTransaction")] // <-- This ensures EF matches the SQL table name
    public class ScheduledTransaction
    {
        [Key]
        public int STId { get; set; }

        [ForeignKey("FromAccount")]
        public int fromAcc { get; set; }

        [ForeignKey("ToAccount")]
        public int toAcc { get; set; }

        public decimal Amount { get; set; }

        public DateTime ScheduleTime { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string TransacStatus { get; set; } = "Pending";
    }
}
