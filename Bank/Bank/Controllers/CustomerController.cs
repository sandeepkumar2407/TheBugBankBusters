using Bank.Models;
using Bank.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Bank.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Customer")]
    public class CustomerController : BaseController
    {
        readonly BankDbContext bankDbContext;
        readonly PasswordService passwordService;

        public CustomerController(BankDbContext _bankDbContext,PasswordService _passwordService)
        {
            bankDbContext = _bankDbContext;
            passwordService = _passwordService;
        }

        ////////// User section starts //////////
        [HttpGet("GetProfile")]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                var userId = GetUserId();
                if (userId == null)
                    return BadRequest(new { message = "Invalid user ID" });

                var user = await bankDbContext.Users
                    .Where(u => u.UserId == userId)
                    .Select(u => new UserDto
                    {
                        UserId = u.UserId,
                        UserName = u.Uname,
                        DoB = u.DoB,
                        UAddress = u.Uaddress,
                        Gender = u.Gender,
                        Mobile = u.Mobile,
                        Email = u.Email,
                        PANCard = u.PANCard,
                        AadharCard = u.AadharCard
                    })
                    .FirstOrDefaultAsync();

                if (user == null)
                    return NotFound(new { message = $"User with ID {userId} not found" });

                return Ok(user);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPatch("UpdateLoginPassword")]
        public async Task<IActionResult> UpdateLoginPassword([FromBody] UpdatePassDto passDto)
        {
            try
            {
                var userId = GetUserId();
                if (userId == null || passDto == null)
                    return BadRequest(new { message = "Invalid request" });

                var user = await bankDbContext.Users.FindAsync(userId);

                if (user == null)
                    return NotFound(new { message = "User not found" });

                bool isUserPass = passwordService.VerifyPassword(user.LoginPassword, passDto.previousPassword);

                if (!isUserPass)
                    return BadRequest(new { message = "Your old password is incorrect" });

                if (passDto.newPassword != passDto.confirmPassword)
                    return BadRequest(new { message = "New password and confirmation do not match" });

                bool isNewPass = passwordService.VerifyPassword(user.LoginPassword,passDto.newPassword);

                if (isNewPass)
                    return BadRequest(new { message = "New password cannot be the same as the old password" });

                user.LoginPassword = passwordService.HashPassword(passDto.newPassword);
                await bankDbContext.SaveChangesAsync();

                return Ok(new { message = "Password updated successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPatch("UpdateTransacPassword")]
        public async Task<IActionResult> UpdateTransacPassword([FromBody] UpdatePassDto passDto)
        {
            try
            {
                var userId = GetUserId();
                if (userId == null || passDto == null)
                    return BadRequest(new { message = "Invalid request" });

                var user = await bankDbContext.Users.FindAsync(userId);
                if (user == null)
                    return NotFound(new { message = "User not found" });

                bool isUserPass = passwordService.VerifyPassword(user.TransactionPassword, passDto.previousPassword);

                if (!isUserPass)
                    return BadRequest(new { message = "Your old password is incorrect" });

                if (passDto.newPassword != passDto.confirmPassword)
                    return BadRequest(new { message = "New password and confirmation do not match" });

                bool isNewPass = passwordService.VerifyPassword(user.TransactionPassword, passDto.newPassword);

                if (isNewPass)
                    return BadRequest(new { message = "New password cannot be the same as the old password" });

                user.TransactionPassword = passwordService.HashPassword(passDto.newPassword);
                await bankDbContext.SaveChangesAsync();
                return Ok(new { message = "Password updated successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        ////////// User section ends //////////


        ////////// Accounts section starts //////////
        [HttpGet("GetYourAccounts")]
        public async Task<IActionResult> GetYourAccounts()
        {
            try
            {
                var userId = GetUserId();
                if (userId == null)
                    return BadRequest(new { message = "Invalid user ID" });

                var accounts = await bankDbContext.Accounts
                    .Where(a => a.UserId == userId)
                    .Include(a => a.IfscCodeNavigation)
                    .Select(a => new AccountCustRespDto
                    {
                        AccNo = a.AccNo,
                        AccType = a.AccType,
                        Balance = a.Balance,
                        DateOfJoining = a.DateOfJoining,
                        IfscCode = a.IfscCode,
                        AccountStatus = a.AccountStatus,
                        BranchName = a.IfscCodeNavigation.BranchName,
                        BranchAddress = a.IfscCodeNavigation.Baddress
                    })
                    .ToListAsync();

                if (accounts == null || accounts.Count == 0)
                    return NotFound(new { message = $"No accounts found for user ID {userId}" });

                return Ok(accounts);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        ////////// Accounts section ends //////////


        //////// Transactions section starts ////////

        [HttpGet("GetTransacsByAcc/{accNo}")]
        public async Task<IActionResult> GetTransacsByAcc(int accNo)
        {
            try
            {
                var transactions = await bankDbContext.Transactions
                    .Where(t => (t.AccNo == accNo || t.ToAcc == accNo))
                    .Select(t => new TransactionResponseDto
                    {
                        AccNo = t.AccNo,
                        TransacId = t.TransacId,
                        TransacType = t.TransacType,
                        Amount = t.Amount,
                        ToAcc = t.ToAcc,
                        TimeStamps = t.TimeStamps,
                        TransacStatus = t.TransacStatus,
                        Comments = t.Comments
                    })
                    .ToListAsync();

                if (transactions.Count == 0)
                    return NotFound(new { message = $"Transactions for account {accNo} not found" });

                return Ok(transactions);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("GetTransactionHistory")]
        public async Task<IActionResult> GetTransactionHistory()
        {
            try
            {
                var userId = GetUserId();
                if (userId == null)
                    return BadRequest(new { message = "Invalid user ID" });

                var userAccNos = await bankDbContext.Accounts
                    .Where(a => a.UserId == userId)
                    .Select(a => a.AccNo)
                    .ToListAsync();

                if (userAccNos == null || userAccNos.Count == 0)
                    return NotFound(new { message = $"No accounts found for user ID {userId}" });

                var transactions = await bankDbContext.Transactions
                    .Where(t => userAccNos.Contains((int)t.AccNo) || userAccNos.Contains((int)t.ToAcc))
                    .OrderByDescending(t => t.TimeStamps)
                    .Select(t => new TransactionResponseDto
                    {
                        AccNo = t.AccNo,
                        TransacId = t.TransacId,
                        TransacType = t.TransacType,
                        Amount = t.Amount,
                        ToAcc = t.ToAcc,
                        TimeStamps = t.TimeStamps,
                        TransacStatus = t.TransacStatus,
                        Comments = t.Comments
                    })
                    .ToListAsync();

                if (transactions.Count == 0)
                    return NotFound(new { message = $"No transactions found for user ID {userId}" });

                return Ok(transactions);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("Transactions/Transfer")]
        public async Task<IActionResult> Transfer([FromBody] CustTransacTransferDto dto)
        {
            try
            {
                if (dto == null)
                    return BadRequest(new { message = "Transaction details are required." });

                if (dto.Amount <= 0)
                    return BadRequest(new { message = "Amount must be greater than zero." });

                if (dto.Amount > 150000)
                    return BadRequest(new { message = "Amount exceeds the transfer limit of ₹150,000." });

                if (dto.FromAcc == dto.ToAcc)
                    return BadRequest(new { message = "Cannot transfer to the same account." });

                var fromAccount = await bankDbContext.Accounts
                    .Include(a => a.User)
                    .FirstOrDefaultAsync(a => a.AccNo == dto.FromAcc);

                var toAccount = await bankDbContext.Accounts
                    .FirstOrDefaultAsync(a => a.AccNo == dto.ToAcc);

                if (fromAccount == null || toAccount == null)
                    return NotFound(new { message = "Invalid account(s)." });

                if (!fromAccount.AccountStatus.Equals("Active", StringComparison.OrdinalIgnoreCase))
                    return BadRequest(new { message = $"Transfer not allowed. From account is {fromAccount.AccountStatus}." });

                if (toAccount.AccountStatus.Equals("Closed", StringComparison.OrdinalIgnoreCase))
                    return BadRequest(new { message = "Cannot transfer to a closed account." });

                bool isTransPass = passwordService.VerifyPassword(fromAccount.User.TransactionPassword, dto.TransactionPass);

                if (!isTransPass)
                    return Unauthorized(new { message = "Invalid transaction password." });

                if (fromAccount.Balance < dto.Amount)
                    return BadRequest(new { message = "Insufficient balance." });

                fromAccount.Balance -= dto.Amount;
                toAccount.Balance += dto.Amount;

                var senderComment = $"Transferred ₹{dto.Amount} to A/C {dto.ToAcc}";
                if (!string.IsNullOrWhiteSpace(dto.Comments))
                    senderComment += $" | Note: {dto.Comments}";

                var transaction = new Transaction
                {
                    AccNo = dto.FromAcc,
                    ToAcc = dto.ToAcc,
                    Amount = dto.Amount,
                    TransacType = "Transfer",
                    TransacStatus = "Completed",
                    TimeStamps = DateTime.Now,
                    Comments = senderComment
                };

                await bankDbContext.Transactions.AddAsync(transaction);
                await bankDbContext.SaveChangesAsync();

                return Ok(new { message = $"Successfully transferred ₹{dto.Amount} from A/C {dto.FromAcc} to A/C {dto.ToAcc}." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("ScheduleTransaction")]
        public async Task<IActionResult> ScheduleTransaction([FromBody] ScheduleTransactionDto dto)
        {
            try
            {
                if (dto == null)
                    return BadRequest(new { message = "Missing transaction data." });

                if (dto.Amount <= 0)
                    return BadRequest(new { message = "Amount must be greater than 0." });

                // Convert incoming time to local (IST) explicitly
                var scheduledTimeUtc = dto.ScheduledTime.Kind == DateTimeKind.Utc
                    ? dto.ScheduledTime
                    : DateTime.SpecifyKind(dto.ScheduledTime, DateTimeKind.Utc);

                var scheduledTimeLocal = scheduledTimeUtc.ToLocalTime();

                Console.WriteLine($"[DEBUG] dto.ScheduledTime={dto.ScheduledTime} (Kind={dto.ScheduledTime.Kind})");
                Console.WriteLine($"[DEBUG] scheduledTimeLocal={scheduledTimeLocal}");
                Console.WriteLine($"[DEBUG] Now (Local)={DateTime.Now}");
                Console.WriteLine($"[DEBUG] Now (UTC)={DateTime.UtcNow}");

                // Validate with local time (IST)
                if (scheduledTimeLocal < DateTime.Now || scheduledTimeLocal > DateTime.Now.AddHours(24))
                    return BadRequest(new { message = "Scheduled time must be within 24 hours from now." });

                var fromAccount = await bankDbContext.Accounts
                    .Include(a => a.User)
                    .FirstOrDefaultAsync(a => a.AccNo == dto.FromAccountId);

                var toAccount = await bankDbContext.Accounts
                    .FirstOrDefaultAsync(a => a.AccNo == dto.ToAccountId);

                if (fromAccount == null || toAccount == null)
                    return BadRequest(new { message = "Invalid account(s)." });

                if (string.IsNullOrEmpty(dto.TransactionPass))
                    return BadRequest(new { message = "Missing transaction password." });

                bool isTransPass = passwordService.VerifyPassword(fromAccount.User.TransactionPassword, dto.TransactionPass);

                if (!isTransPass)
                    return Unauthorized(new { message = "Invalid transaction password." });

                // Save in UTC for consistency
                var scheduled = new ScheduledTransaction
                {
                    fromAcc = dto.FromAccountId,
                    toAcc = dto.ToAccountId,
                    Amount = dto.Amount,
                    ScheduleTime = scheduledTimeUtc
                    //ScheduleTime = dto.ScheduledTime.ToUniversalTime()
                };

                await bankDbContext.ScheduledTransactions.AddAsync(scheduled);
                await bankDbContext.SaveChangesAsync();

                return Ok(new { message = "Transaction scheduled successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("ScheduledTransactions")]
        public async Task<IActionResult> GetScheduledTransactions()
        {
            try
            {
                var userId = GetUserId();
                if (userId == null)
                    return BadRequest(new { message = "Invalid user ID" });

                var userAccNos = await bankDbContext.Accounts
                    .Where(a => a.UserId == userId)
                    .Select(a => a.AccNo)
                    .ToListAsync();

                if (userAccNos == null || userAccNos.Count == 0)
                    return NotFound(new { message = "No accounts found for this user." });

                // Fetch only scheduled transactions linked to user's accounts
                var list = await bankDbContext.ScheduledTransactions
                    .Where(t => userAccNos.Contains(t.fromAcc) || userAccNos.Contains(t.toAcc))
                    .OrderByDescending(t => t.CreatedAt)
                    .Select(t => new
                    {
                        t.STId,
                        t.fromAcc,
                        t.toAcc,
                        t.Amount,
                        t.ScheduleTime,
                        t.CreatedAt,
                        t.TransacStatus
                    })
                    .ToListAsync();

                if (list.Count == 0)
                    return NotFound(new { message = "No scheduled transactions found for your accounts." });

                return Ok(list);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }


        [HttpPatch("CancelScheduledTransaction/{id}")]
        public async Task<IActionResult> CancelScheduledTransaction(int id, [FromBody] CancelTransactionDto dto)
        {
            try
            {
                if (dto == null || string.IsNullOrEmpty(dto.TransactionPass))
                    return BadRequest(new { message = "Transaction password is required." });

                var scheduledTx = await bankDbContext.ScheduledTransactions.FindAsync(id);
                if (scheduledTx == null)
                    return NotFound(new { message = "Scheduled transaction not found." });

                if (scheduledTx.TransacStatus == "Executed" || scheduledTx.TransacStatus == "Failed")
                    return BadRequest(new { message = "This transaction cannot be cancelled as it is already processed." });

                if (scheduledTx.ScheduleTime <= DateTime.Now)
                    return BadRequest(new { message = "Cannot cancel a transaction whose scheduled time has already passed." });

                var fromAccount = await bankDbContext.Accounts
                    .Include(a => a.User)
                    .FirstOrDefaultAsync(a => a.AccNo == scheduledTx.fromAcc);

                if (fromAccount == null)
                    return BadRequest(new { message = "Linked account not found." });

                bool isTransPass = passwordService.VerifyPassword(fromAccount.User.TransactionPassword, dto.TransactionPass);

                if (!isTransPass)
                    return Unauthorized(new { message = "Invalid transaction password." });

                scheduledTx.TransacStatus = "Cancelled";
                bankDbContext.ScheduledTransactions.Update(scheduledTx);
                await bankDbContext.SaveChangesAsync();

                return Ok(new { message = "Scheduled transaction cancelled successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        //////// Transactions section ends ////////
    }
}