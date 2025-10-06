namespace Bank.Models
{
    public class UserCreationDto
    {
        public string UserName { get; set; } = null!;

        public DateOnly DoB { get; set; }

        public string? UAddress { get; set; }

        public string Gender { get; set; } = null!;

        public string Mobile { get; set; } = null!;

        public string Email { get; set; } = null!;

        public string PANCard { get; set; } = null!;

        public string AadharCard { get; set; } = null!;
    }
}
