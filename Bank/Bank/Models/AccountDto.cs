namespace Bank.Models
{
    public class AccountDto
    {
        public int UserId { get; set; }

        public string AccType { get; set; } = null!;

        public decimal Balance { get; set; }

        public DateTime DateOfJoining { get; set; }

        public int BranchId { get; set; }
    }
    public class AccountUpdateDto
    {
        public int AccNo { get; set; }

        public string ?AccType { get; set; }

        public decimal ?Balance { get; set; }

        public string ?IfscCode { get; set; }
    }
    public class AccountGetDto
    {
        public int AccNo { get; set; }

        public int UserId { get; set; }

        public string Uname { get; set; }

        public string AccType { get; set; }

        public decimal Balance { get; set; }

        public DateTime DateOfJoining { get; set; }

        public string IfscCode { get; set; }
    }

}
