namespace Bank.Models
{
    public class EmpUpdateDto
    {
        public string ?EmpName { get; set; } 

        public string ?EmpRole { get; set; }

        public string ?EmpMobile { get; set; } 

        public string ?EmpEmail { get; set; }

        public int ?BranchId { get; set; }
    }
}
