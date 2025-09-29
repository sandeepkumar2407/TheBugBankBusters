namespace Bank.Models
{
    public class UserDto
    {
        public string Uname { get; set; } = null!;

        public DateOnly DoB { get; set; }

        public string? Uaddress { get; set; }

        public string Gender { get; set; } = null!;

        public string Mobile { get; set; } = null!;

        public string Email { get; set; } = null!;
    }
    public class UserUpdateDto
    {
        public string ?Uname { get; set; }

        public string? Uaddress { get; set; }

        public string ?Mobile { get; set; }

        public string ?Email { get; set; }
    }
    public class UserGetDto
    {
        public int UserId { get; set; }
        public string Uname { get; set; } = null!;

        public DateOnly DoB { get; set; }

        public string? Uaddress { get; set; }

        public string Gender { get; set; } = null!;

        public string Mobile { get; set; } = null!;

        public string Email { get; set; } = null!;
    }
}
