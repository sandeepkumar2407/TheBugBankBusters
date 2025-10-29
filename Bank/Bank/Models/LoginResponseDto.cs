namespace Bank.Models
{
    public class LoginResponseDto
    {
        public string Token { get; set; } = null!;
        public string Message { get; set; } = null!;
        public string Role { get; set; } = null!;
        public int? UserId { get; set; }
        public int? BranchId { get; set; }
        public string? BranchName { get; set; }
    }
}
