using System;
using System.Collections.Generic;

namespace Bank.Models;

public partial class Transaction
{
    public long TransacId { get; set; }

    public string TransacType { get; set; } = null!;

    public decimal Amount { get; set; }

    public DateTime TimeStamps { get; set; }

    public string TransacStatus { get; set; } = null!;

    public int? AccNo { get; set; }

    public int? ToAcc { get; set; }

    public string? Comments { get; set; }

    public virtual Account? AccNoNavigation { get; set; }

    public virtual Account? ToAccNavigation { get; set; }
}
