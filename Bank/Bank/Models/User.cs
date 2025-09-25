using System;
using System.Collections.Generic;

namespace Bank.Models;

public partial class User
{
    public int UserId { get; set; }

    public string Uname { get; set; } = null!;

    public DateOnly DoB { get; set; }

    public string? Uaddress { get; set; }

    public string Gender { get; set; } = null!;

    public string Mobile { get; set; } = null!;

    public string Email { get; set; } = null!;

    public virtual ICollection<Account> Accounts { get; set; } = new List<Account>();
}
