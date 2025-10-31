using Bank.Models;
using Microsoft.EntityFrameworkCore;

namespace Bank.Services
{
    public class ScheduledTransactionService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ScheduledTransactionService> _logger;
        public ScheduledTransactionService(IServiceProvider serviceProvider, ILogger<ScheduledTransactionService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await ProcessScheduledTransactions();
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
        private async Task ProcessScheduledTransactions()
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<BankDbContext>();

            var dueTransactions = await db.ScheduledTransactions
                .Where(t => t.TransacStatus == "Pending" && t.ScheduleTime <= DateTime.Now)
                .ToListAsync();

            foreach (var t in dueTransactions)
            {
                using var transaction = await db.Database.BeginTransactionAsync();
                try
                {
                    var from = await db.Accounts.FindAsync(t.fromAcc);
                    var to = await db.Accounts.FindAsync(t.toAcc);

                    if (from == null || to == null)
                    {
                        t.TransacStatus = "Failed";
                        continue;
                    }

                    if (from.Balance < t.Amount)
                    {
                        t.TransacStatus = "Failed";
                        continue;
                    }

                    from.Balance -= t.Amount;
                    to.Balance += t.Amount;

                    var newTransaction = new Transaction
                    {
                        AccNo = t.fromAcc,
                        ToAcc = t.toAcc,
                        Amount = t.Amount,
                        TimeStamps = DateTime.Now,
                        TransacType = "Transfer",
                        TransacStatus = "Completed",
                        Comments = $"Scheduled transaction from {t.fromAcc} account to {t.toAcc} account"
                    };

                    db.Transactions.Add(newTransaction);
                    t.TransacStatus = "Executed";
                    await db.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to execute scheduled transaction {0}", t.STId);
                    t.TransacStatus = "Failed";
                    await db.SaveChangesAsync();
                }
            }

            await db.SaveChangesAsync();
        }
    }
}
