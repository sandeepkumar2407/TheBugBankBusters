namespace Bank.Models
{
    public class UpdatePassDto
    {
        public string previousPassword { get; set; } = null!;
        public string newPassword { get; set; } = null!;
        public string confirmPassword { get; set; } = null!;
    }
}
