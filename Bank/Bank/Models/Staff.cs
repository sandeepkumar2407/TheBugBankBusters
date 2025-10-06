using System;
using System.Collections.Generic;

namespace Bank.Models;

public partial class Staff
{
    public int EmpId { get; set; }

    public string EmpName { get; set; } = null!;

    public string EmpRole { get; set; } = null!;

    public string EmpMobile { get; set; } = null!;

    public string EmpEmail { get; set; } = null!;

    public string EmpPass { get; set; } = null!;

    public int? BranchId { get; set; } = null!;

    public virtual Branch? Branch { get; set; }
}
