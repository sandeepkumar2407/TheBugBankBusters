using System;
using System.Collections.Generic;

namespace Bank.Models;

public partial class Account
{
    public int AccNo { get; set; }

    public int UserId { get; set; }

    public string AccType { get; set; } = null!;

    public decimal Balance { get; set; }

    public DateTime DateOfJoining { get; set; }

    public string IfscCode { get; set; } = null!;

    public string AccountStatus { get; set; } = null!;

    public virtual Branch IfscCodeNavigation { get; set; } = null!;

    public virtual ICollection<Transaction> TransactionAccNoNavigations { get; set; } = new List<Transaction>();

    public virtual ICollection<Transaction> TransactionToAccNavigations { get; set; } = new List<Transaction>();

    public virtual User User { get; set; } = null!;
}
