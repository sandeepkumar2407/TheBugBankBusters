using Bank.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Bank.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles="Customer")]
    public class CustomerController : BaseController
    {
        readonly BankDbContext bankDbContext;
        public CustomerController(BankDbContext _bankDbContext)
        {
            this.bankDbContext = _bankDbContext;
        }

        //////////User section starts ///////////
        [HttpGet("GetProfile")]
        public IActionResult GetProfile()
        {
            try
            {
                var userid = GetUserId();
                var user = bankDbContext.Users
                    .Where(u => u.UserId == userid)
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
                    .FirstOrDefault();

                if (user == null)
                    return NotFound($"User with ID {userid} not found");

                return Ok(user);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPatch("UpdateLoginPassword")]
        public IActionResult UpdateLoginPassword(UpdatePassDto passDto)
        {
            try
            {
                var userId = GetUserId();
                if (passDto == null)
                    return BadRequest("Invalid request");

                var user = bankDbContext.Users.Find(userId);
                if (user == null)
                    return NotFound("User not found for this user ID");

                if (!VerifyPassword(passDto.previousPassword, user.LoginPassword))
                    return BadRequest("Your old password is incorrect");

                if (passDto.newPassword != passDto.confirmPassword)
                    return BadRequest("New password and confirmation do not match");

                if (VerifyPassword(passDto.newPassword, user.LoginPassword))
                    return BadRequest("New password cannot be the same as the old password");

                user.LoginPassword = passDto.newPassword;
                bankDbContext.SaveChanges();

                return Ok("Password updated successfully");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error updating password: {ex.Message}");
            }
        }
        [HttpPatch("UpdateTransacPassword")]
        public IActionResult UpdateTransacPassword(UpdatePassDto passDto)
        {
            try
            {
                var userId = GetUserId();
                if (passDto == null)
                    return BadRequest("Invalid request");

                var user = bankDbContext.Users.Find(userId);
                if (user == null)
                    return NotFound("User not found for this user ID");

                if (!VerifyPassword(passDto.previousPassword, user.TransactionPassword))
                    return BadRequest("Your old password is incorrect");

                if (VerifyPassword(passDto.newPassword, user.TransactionPassword))
                    return BadRequest("New password cannot be the same as the old password");

                if (passDto.newPassword != passDto.confirmPassword)
                    return BadRequest("New password and confirmation do not match");

                user.TransactionPassword = passDto.newPassword;
                bankDbContext.SaveChanges();

                return Ok("Password updated successfully");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error updating password: {ex.Message}");
            }
        }

        //////////User section ends ///////////
        //////////Accounts section starts ///////////

        [HttpGet("GetYourAccounts")]
        public IActionResult GetYourAccounts()
        {
            try
            {
                var userId = GetUserId();
                if(userId < 100)
                {
                    return BadRequest("Invalid user ID");
                }
                // Fetch accounts with related Branch info
                var accounts = bankDbContext.Accounts
                    .Where(a => a.UserId == userId)
                    .Include(a => a.IfscCodeNavigation) // includes Branch info
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
                    .ToList();

                // Handle if no accounts found
                if (accounts == null || accounts.Count == 0)
                {
                    return NotFound($"No accounts found for user ID {userId}");
                }

                // Return DTO response
                return Ok(accounts);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error fetching accounts: {ex.Message}");
            }
        }
        //////////Accounts section ends ///////////

        ////////Transactions part starts /////////
        private void SaveTransaction(int? fromAcc, int? toAcc, decimal amount, string type, string status, string? comments)
        {
            var transaction = new Transaction
            {
                AccNo = fromAcc,
                ToAcc = toAcc,
                Amount = amount,
                TransacType = type,
                TransacStatus = status,
                TimeStamps = DateTime.Now,
                Comments = comments
            };
            bankDbContext.Transactions.Add(transaction);
        }
        private bool VerifyPassword(string? inputPassword, string hashedPassword)
        {
            //return BCrypt.Net.BCrypt.Verify(inputPassword, hashedPassword);
            return inputPassword == hashedPassword;
            //return true;
        }

        [HttpGet("GetTransacsByAcc/{accNo}")]
        public IActionResult GetTransacsByAcc(int accNo)
        {
            try
            {
                var transactions = bankDbContext.Transactions
                    .Where(t => t.AccNo == accNo)
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
                    }).ToList();

                if (transactions.Count == 0)
                {
                    return NotFound($"Transactions for account {accNo} not found");
                }
                return Ok(transactions);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("GetTransactionHistory")]
        public IActionResult GetTransactionHistory()
        {
            try
            {
                var userId = GetUserId();
                // Step 1: Get all account numbers for the user
                var userAccNos = bankDbContext.Accounts
                    .Where(a => a.UserId == userId)
                    .Select(a => a.AccNo)
                    .ToList();

                if (userAccNos == null || userAccNos.Count == 0)
                {
                    return NotFound($"No accounts found for user ID {userId}");
                }

                // Step 2: Get all transactions related to those accounts
                var transactions = bankDbContext.Transactions
                    .Where(t => userAccNos.Contains((int)t.AccNo) || userAccNos.Contains((int)t.ToAcc))
                    .OrderByDescending(t => t.TimeStamps) // optional: sort by latest first
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
                    .ToList();

                if (transactions == null || transactions.Count == 0)
                {
                    return NotFound($"No transactions found for user ID {userId}");
                }

                // Step 3: Return all transactions
                return Ok(transactions);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error fetching transactions: {ex.Message}");
            }
        }

        [HttpPost("Transactions/Transfer")]
        public IActionResult Transfer([FromBody] CustTransacTransferDto dto)
        {
            try
            {
                if (dto == null)
                    return BadRequest("Transaction details are required.");

                if (dto.Amount <= 0)
                {
                    //SaveTransaction(dto.FromAcc, dto.ToAcc, dto.Amount, "Transfer", "Failed", "Invalid amount");
                    //bankDbContext.SaveChanges();
                    return BadRequest("Amount must be greater than zero.");
                }

                if(dto.Amount > 150000)
                {
                    return BadRequest("Amount exceeds the transfer limit of ₹150,000.");
                }

                if (dto.FromAcc == dto.ToAcc)
                {
                    SaveTransaction(dto.FromAcc, dto.ToAcc, dto.Amount, "Transfer", "Failed", "Same source and destination account");
                    bankDbContext.SaveChanges();
                    return BadRequest("Cannot transfer to the same account.");
                }

                var fromAccount = bankDbContext.Accounts
                    .Include(a => a.User)
                    .FirstOrDefault(a => a.AccNo == dto.FromAcc);

                var toAccount = bankDbContext.Accounts
                    .FirstOrDefault(a => a.AccNo == dto.ToAcc);

                if (fromAccount == null || toAccount == null)
                {
                    //SaveTransaction(dto.FromAcc, dto.ToAcc, dto.Amount, "Transfer", "Failed", "Invalid account(s)");
                    //bankDbContext.SaveChanges();
                    return NotFound("Invalid account(s).");
                }

                if (!fromAccount.AccountStatus.Equals("Active", StringComparison.OrdinalIgnoreCase))
                {
                    SaveTransaction(dto.FromAcc, dto.ToAcc, dto.Amount, "Transfer", "Failed", $"Sender account is {fromAccount.AccountStatus}");
                    bankDbContext.SaveChanges();
                    return BadRequest($"Transfer not allowed. From account is {fromAccount.AccountStatus}.");
                }

                if (toAccount.AccountStatus.Equals("Closed", StringComparison.OrdinalIgnoreCase))
                {
                    SaveTransaction(dto.FromAcc, dto.ToAcc, dto.Amount, "Transfer", "Failed", "Receiver account is closed");
                    bankDbContext.SaveChanges();
                    return BadRequest("Cannot transfer to a closed account.");
                }

                if (!VerifyPassword(dto.TransactionPass, fromAccount.User.TransactionPassword))
                {
                    SaveTransaction(dto.FromAcc, dto.ToAcc, dto.Amount, "Transfer", "Failed", "Incorrect transaction password");
                    bankDbContext.SaveChanges();
                    return Unauthorized("Invalid transaction password.");
                }

                if (fromAccount.Balance < dto.Amount)
                {
                    SaveTransaction(dto.FromAcc, dto.ToAcc, dto.Amount, "Transfer", "Failed", "Insufficient balance");
                    bankDbContext.SaveChanges();
                    return BadRequest("Insufficient balance.");
                }

                fromAccount.Balance -= dto.Amount;
                toAccount.Balance += dto.Amount;

                string senderComment = $"Transferred Rs.{dto.Amount} to A/C {dto.ToAcc}";

                if (!string.IsNullOrWhiteSpace(dto.Comments))
                {
                    senderComment += $" | Note: {dto.Comments}";
                }

                SaveTransaction(dto.FromAcc, dto.ToAcc, dto.Amount, "Transfer", "Completed", senderComment);

                bankDbContext.SaveChanges();

                return Ok($"Successfully transferred ₹{dto.Amount} from A/C {dto.FromAcc} to A/C {dto.ToAcc}.");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error: {ex.Message}");
            }
        }
    }
}