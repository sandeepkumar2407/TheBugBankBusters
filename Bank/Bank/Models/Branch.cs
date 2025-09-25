using System;
using System.Collections.Generic;

namespace Bank.Models;

public partial class Branch
{
    public int BranchId { get; set; }

    public string BranchName { get; set; } = null!;

    public string? Baddress { get; set; }

    public string IfscCode { get; set; } = null!;

    public virtual ICollection<Account> Accounts { get; set; } = new List<Account>();

    public virtual ICollection<Staff> Staff { get; set; } = new List<Staff>();
}
