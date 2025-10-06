namespace Bank.Models
{
    public class AccountDto
    {
        public int UserId { get; set; }
        public string AccType { get; set; } = null!;
        public decimal Balance { get; set; }
        //public string IfscCode { get; set; } = null!;
    }

}
