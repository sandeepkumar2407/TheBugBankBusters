using Bank.Models;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace Bank.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ManagerController : ControllerBase
    {
        readonly BankDbContext bankDbContext;

        public ManagerController(BankDbContext _bankDbContext)
        {
            this.bankDbContext = _bankDbContext;
        }

        //////CUSTOMER PART///////

        private static string GenerateRandomPassword(int length)
        {
            const string validChars = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789@#$&";
            Random random = new Random();
            return new string(Enumerable.Repeat(validChars, length)
                                        .Select(s => s[random.Next(s.Length)])
                                        .ToArray());
        }

        [HttpGet("Users")]
        public IActionResult GetAllUsers()
        {
            try
            {
                var users = bankDbContext.Users.Select(u => new UserDto
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
                }).ToList();

                if (users.Count == 0)
                    return NotFound("No users found");

                return Ok(users);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("Users/{id}")]
        public IActionResult GetUserById(int id)
        {
            try
            {
                var user = bankDbContext.Users
                    .Where(u => u.UserId == id)
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
                    return NotFound($"User with ID {id} not found");

                return Ok(user);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("AddUser")]
        public IActionResult AddNewUser(UserCreationDto newUser)
        {
            try
            {
                if (newUser == null)
                    return BadRequest("Invalid user data");

                string generatedPassword = GenerateRandomPassword(12);

                User u = new User()
                {
                    Uname = newUser.UserName,
                    DoB = newUser.DoB,
                    Uaddress = newUser.UAddress,
                    Gender = newUser.Gender,
                    Mobile = newUser.Mobile,
                    Email = newUser.Email,
                    PANCard = newUser.PANCard,
                    AadharCard = newUser.AadharCard,
                    LoginPassword = generatedPassword,
                    TransactionPassword = generatedPassword
                };
                bankDbContext.Users.Add(u);
                bankDbContext.SaveChanges();

                return Ok("User added to the bank");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("UpdateUser/{id}")]
        public IActionResult UpdateUser(int id, UserUpdateDto userUpdate)
        {
            try
            {
                if (userUpdate == null)
                    return BadRequest("Invalid details");

                var user = bankDbContext.Users.Find(id);
                if (user == null)
                    return NotFound($"User with ID {id} not found");

                if (!string.IsNullOrEmpty(userUpdate.UserName)) user.Uname = userUpdate.UserName;
                if (!string.IsNullOrEmpty(userUpdate.Email)) user.Email = userUpdate.Email;
                if (!string.IsNullOrEmpty(userUpdate.UAddress)) user.Uaddress = userUpdate.UAddress;
                if (!string.IsNullOrEmpty(userUpdate.Mobile)) user.Mobile = userUpdate.Mobile;

                bankDbContext.SaveChanges();
                return Ok("User details updated successfully");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("DeleteUser/{id}")]
        public IActionResult DeleteUser(int id)
        {
            try
            {
                var user = bankDbContext.Users.Find(id);
                if (user == null)
                    return NotFound($"User with ID {id} not found");

                bankDbContext.Users.Remove(user);
                bankDbContext.SaveChanges();

                return Ok("User removed successfully");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        ///////TRANSACTIONS PART///////

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

        [HttpGet("Transactions")]
        public IActionResult GetAllTransactions(int? accNo)
        {
            try
            {
                if (accNo == null)
                    return BadRequest("Enter account number");

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
                    return NotFound($"Transactions for account {accNo} not found");

                return Ok(transactions);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("Transactions/{id}")]
        public IActionResult GetTransactionById(long id)
        {
            try
            {
                var trans = bankDbContext.Transactions
                    .Where(t => t.TransacId == id)
                    .Select(t => new TransactionResponseDto
                    {
                        TransacId = t.TransacId,
                        TransacType = t.TransacType,
                        Amount = t.Amount,
                        TimeStamps = t.TimeStamps,
                        TransacStatus = t.TransacStatus,
                        AccNo = t.AccNo,
                        ToAcc = t.ToAcc,
                        Comments = t.Comments
                    })
                    .FirstOrDefault();

                if (trans == null)
                    return NotFound($"Transaction with ID {id} not found");

                return Ok(trans);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("Transactions/CreditOrDebit")]
        public IActionResult CreditOrDebit(TransactionCreditDebitDto creditDebitDto)
        {
            try
            {
                if (creditDebitDto == null)
                    return BadRequest("Enter transaction details");

                var account = bankDbContext.Accounts.Find(creditDebitDto.AccNo);
                if (account == null)
                {
                    SaveTransaction(creditDebitDto.AccNo, null, creditDebitDto.Amount, creditDebitDto.TransacType, "Failed", "Invalid account");
                    bankDbContext.SaveChanges();
                    return NotFound("Account not found");
                }

                if (creditDebitDto.Amount <= 0)
                {
                    SaveTransaction(creditDebitDto.AccNo, null, creditDebitDto.Amount, creditDebitDto.TransacType, "Failed", "Invalid amount");
                    bankDbContext.SaveChanges();
                    return BadRequest("Amount must be greater than zero");
                }

                if (creditDebitDto.TransacType.Equals("Credit", StringComparison.OrdinalIgnoreCase))
                    account.Balance += creditDebitDto.Amount;
                else if (creditDebitDto.TransacType.Equals("Debit", StringComparison.OrdinalIgnoreCase))
                {
                    if (account.Balance < creditDebitDto.Amount)
                    {
                        SaveTransaction(creditDebitDto.AccNo, null, creditDebitDto.Amount, "Debit", "Failed", "Insufficient balance");
                        bankDbContext.SaveChanges();
                        return BadRequest("Insufficient balance");
                    }
                    account.Balance -= creditDebitDto.Amount;
                }
                else
                    return BadRequest("Invalid transaction type");

                var transaction = new Transaction
                {
                    TransacType = creditDebitDto.TransacType,
                    Amount = creditDebitDto.Amount,
                    AccNo = creditDebitDto.AccNo,
                    ToAcc = null,
                    TimeStamps = DateTime.Now,
                    TransacStatus = "Completed",
                    Comments = creditDebitDto.Comments ?? ""
                };

                bankDbContext.Transactions.Add(transaction);
                bankDbContext.SaveChanges();

                return Ok($"Transaction {creditDebitDto.TransacType} of {creditDebitDto.Amount} successful");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("Transactions/Transfer")]
        public IActionResult Transfer(TransactionTransferDto transferDto)
        {
            try
            {
                if (transferDto == null)
                    return BadRequest("Enter transaction details");

                var fromAccount = bankDbContext.Accounts.Find(transferDto.FromAcc);
                var toAccount = bankDbContext.Accounts.Find(transferDto.ToAcc);

                if (fromAccount == null || toAccount == null)
                    return NotFound("Invalid account(s)");

                if (transferDto.Amount <= 0)
                    return BadRequest("Amount must be greater than zero");

                if (transferDto.FromAcc == transferDto.ToAcc)
                    return BadRequest("Cannot transfer to the same account");

                if (fromAccount.Balance < transferDto.Amount)
                    return BadRequest("Insufficient balance");

                fromAccount.Balance -= transferDto.Amount;
                toAccount.Balance += transferDto.Amount;

                var transaction = new Transaction
                {
                    TransacType = "Transfer",
                    Amount = transferDto.Amount,
                    AccNo = transferDto.FromAcc,
                    ToAcc = transferDto.ToAcc,
                    TimeStamps = DateTime.Now,
                    TransacStatus = "Completed",
                    Comments = transferDto.Comments ?? ""
                };
                bankDbContext.Transactions.Add(transaction);
                bankDbContext.SaveChanges();

                return Ok($"Transferred {transferDto.Amount} successfully");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("Transactions/{id}")]
        public IActionResult DeleteTransaction(long id)
        {
            try
            {
                var transaction = bankDbContext.Transactions.Find(id);
                if (transaction == null)
                    return NotFound($"Transaction {id} not found");

                if (!transaction.TransacStatus.Equals("Failed", StringComparison.OrdinalIgnoreCase))
                    return BadRequest("Only failed transactions can be deleted");

                bankDbContext.Transactions.Remove(transaction);
                bankDbContext.SaveChanges();

                return Ok("Failed transaction deleted successfully");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        //////////ACCOUNTS PART//////

        [HttpGet("Accounts/{id}")]
        public IActionResult GetAccountById(int id)
        {
            try
            {
                var account = (from a in bankDbContext.Accounts
                               join u in bankDbContext.Users on a.UserId equals u.UserId
                               join b in bankDbContext.Branches on a.IfscCode equals b.IfscCode
                               where a.AccNo == id
                               select new AccountResponseDto
                               {
                                   AccNo = a.AccNo,
                                   AccType = a.AccType,
                                   Balance = a.Balance,
                                   DateOfJoining = a.DateOfJoining,
                                   IfscCode = a.IfscCode,
                                   AccountStatus = a.AccountStatus,
                                   UserId = u.UserId,
                                   UserName = u.Uname,
                                   Email = u.Email,
                                   Mobile = u.Mobile,
                                   BranchId = b.BranchId,
                                   BranchName = b.BranchName,
                                   BranchAddress = b.Baddress
                               }).FirstOrDefault();

                if (account == null)
                    return NotFound($"Account with ID {id} not found");

                return Ok(account);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("CreateAccount")]
        public IActionResult CreateAccount(AccountDto accountDto)
        {
            try
            {
                if (accountDto == null)
                    return BadRequest("Enter correct data");

                var user = bankDbContext.Users.Find(accountDto.UserId);
                if (user == null)
                    return NotFound("User not found");

                var branch = bankDbContext.Branches.FirstOrDefault();
                if (branch == null)
                    return NotFound("Branch not found");

                Account acc = new Account
                {
                    UserId = accountDto.UserId,
                    AccType = accountDto.AccType,
                    Balance = accountDto.Balance,
                    DateOfJoining = DateTime.UtcNow,
                    IfscCode = branch.IfscCode,
                    AccountStatus = "Active"
                };

                bankDbContext.Accounts.Add(acc);
                bankDbContext.SaveChanges();

                return Ok("Account created successfully");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        //////////STAFF PART//////

        [HttpGet("GetAllStaff")]
        public IActionResult GetAllStaff()
        {
            try
            {
                var emps = bankDbContext.Staff.Select(s => new StaffResponseDto
                {
                    EmpID = s.EmpId,
                    EmpName = s.EmpName,
                    EmpRole = s.EmpRole,
                    EmpMobile = s.EmpMobile,
                    EmpEmail = s.EmpEmail,
                    BranchId = s.BranchId
                }).ToList();

                if (emps.Count == 0)
                    return NotFound("No staff found");

                return Ok(emps);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("GetStaffByID/{id}")]
        public IActionResult GetStaffByID(int id)
        {
            try
            {
                var emp = bankDbContext.Staff
                    .Where(s => s.EmpId == id)
                    .Select(s => new StaffResponseDto
                    {
                        EmpID = s.EmpId,
                        EmpName = s.EmpName,
                        EmpRole = s.EmpRole,
                        EmpMobile = s.EmpMobile,
                        EmpEmail = s.EmpEmail,
                        BranchId = s.BranchId
                    })
                    .FirstOrDefault();

                if (emp == null)
                    return NotFound($"Staff with ID {id} not found");

                return Ok(emp);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}