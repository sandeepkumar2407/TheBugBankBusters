using Bank.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Bank.Controllers
{
    [Authorize(Roles = "Staff")]
    [Route("api/[controller]")]
    [ApiController]
    public class StaffController : ControllerBase
    {
        readonly BankDbContext bankDbContext;

        public StaffController(BankDbContext _bankDbContext)
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

        //[HttpGet("Users")]
        //public IActionResult GetAllUsers()
        //{
        //    try
        //    {
        //        var users = bankDbContext.Users
        //                    .Select(u => new UserDto
        //                    {
        //                        UserId = u.UserId,
        //                        UserName = u.Uname,
        //                        DoB = u.DoB,
        //                        UAddress = u.Uaddress,
        //                        Gender = u.Gender,
        //                        Mobile = u.Mobile,
        //                        Email = u.Email,
        //                        PANCard = u.PANCard,
        //                        AadharCard = u.AadharCard,
        //                    })
        //                    .ToList();

        //        if (users.Count == 0)
        //        {
        //            return NotFound("There are no users for your bank!");
        //        }
        //        return Ok(users);
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(ex.Message);
        //    }
        //}

        [HttpGet("Users")]
        public IActionResult GetAllUsers()
        {
            try
            {
                //Get branch ID from session
                //var branchId = HttpContext.Session.GetInt32("BranchId");
                //if (branchId == null)
                //{
                //    return Unauthorized("Login required to view users");
                //}

                var branchIdClaim = User.Claims.FirstOrDefault(c => c.Type == "BranchId")?.Value;

                if (branchIdClaim == null)
                {
                    return Unauthorized("Login required to view users");
                }

                int branchId = int.Parse(branchIdClaim);

                //Get IFSC of logged-in branch
                var branch = bankDbContext.Branches.FirstOrDefault(b => b.BranchId == branchId);
                if (branch == null)
                {
                    return NotFound("Branch not found for the logged-in Staff");
                }

                //Fetch only users whose accounts belong to this branch
                var users = (from u in bankDbContext.Users
                             join a in bankDbContext.Accounts on u.UserId equals a.UserId
                             where a.IfscCode == branch.IfscCode
                             select new UserDto
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
                             .Distinct()
                             .ToList();

                if (users.Count == 0)
                {
                    return NotFound("There are no users for your branch!");
                }

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
                {
                    return NotFound($"User with {id} not found");
                }
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
                {
                    return BadRequest("User Data is not valid");
                }

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
                return Ok("User Added to the Bank");
            }
            catch(Exception ex)
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
                {
                    return BadRequest("Details Invalid");
                }
                if (id < 100)
                {
                    return BadRequest("Enter Valid UserID");
                }
                var user = bankDbContext.Users.Find(id);
                if (user == null)
                {
                    return NotFound($"User with {id} Not Found");
                }
                if(!string.IsNullOrEmpty(userUpdate.UserName))
                {
                    user.Uname = userUpdate.UserName;
                }
                if(!string.IsNullOrEmpty(userUpdate.Email))
                {
                    user.Email = userUpdate.Email;
                }
                if (!string.IsNullOrEmpty(userUpdate.UAddress))
                {
                    user.Uaddress = userUpdate.UAddress;
                }
                if (!string.IsNullOrEmpty(userUpdate.Mobile))
                {
                    user.Mobile = userUpdate.Mobile ;
                }
                bankDbContext.SaveChanges();
                return Ok("User Details Updated Successfully");
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
                if (id < 100)
                {
                    return BadRequest("Enter Valid UserID");
                }
                var user = bankDbContext.Users.Find(id);
                if(user == null)
                {
                    return NotFound($"Usere with User ID {id} not found");
                }
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
                if(accNo == null || accNo < 1000000)
                {
                    return BadRequest("Enter the account number");
                }
                var AccNo = bankDbContext.Accounts.Find(accNo);
                if(AccNo == null)
                {
                    return NotFound("Bank doesn't have this account");
                }
                var transactions = bankDbContext.Transactions
                            .Where(t=>t.AccNo==accNo)
                            .Select(t=>new TransactionResponseDto
                            {
                                AccNo = t.AccNo,
                                TransacId = t.TransacId,
                                TransacType = t.TransacType,
                                Amount = t.Amount,
                                ToAcc = t.ToAcc,
                                TimeStamps = t.TimeStamps,
                                TransacStatus = t.TransacStatus,
                                Comments = t.Comments,
                            }).ToList();
                if(transactions.Count == 0)
                {
                    return NotFound($"Transactions of account {accNo} not found");
                }
                return Ok(transactions);
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("Transactions/{id}")]
        public IActionResult GetTransactionById(long id)
        {
            try
            {
                if (id < 1000000000)
                {
                    return BadRequest("Enter valid transaction ID");
                }

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
                                Comments = t.Comments,
                            }).FirstOrDefault();

                if (trans == null)
                {
                    return NotFound($"Transaction with {id} not found");
                }
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
                {
                    return BadRequest("Enter the transaction details");
                }

                var account = bankDbContext.Accounts.Find(creditDebitDto.AccNo);
                if (account == null)
                {
                    SaveTransaction(creditDebitDto.AccNo, null, creditDebitDto.Amount, creditDebitDto.TransacType, "Failed", "Invalid Account");
                    bankDbContext.SaveChanges();
                    return NotFound("The account is not found in the bank. Please enter a correct account number");
                }

                if (creditDebitDto.TransacType.Equals("Credit", StringComparison.OrdinalIgnoreCase))
                {
                    if (account.AccountStatus.Equals("Closed", StringComparison.OrdinalIgnoreCase))
                    {
                        SaveTransaction(account.AccNo, null, creditDebitDto.Amount, "Credit", "Failed", "Account is closed");
                        bankDbContext.SaveChanges();
                        return BadRequest("Cannot credit to a closed account");
                    }
                }
                else if (creditDebitDto.TransacType.Equals("Debit", StringComparison.OrdinalIgnoreCase))
                {
                    if (!account.AccountStatus.Equals("Active", StringComparison.OrdinalIgnoreCase))
                    {
                        SaveTransaction(account.AccNo, null, creditDebitDto.Amount, "Debit", "Failed", "Account not active");
                        bankDbContext.SaveChanges();
                        return BadRequest("Debit allowed only for active accounts");
                    }
                }

                if (creditDebitDto.Amount <= 0)
                {
                    SaveTransaction(creditDebitDto.AccNo, null, creditDebitDto.Amount, creditDebitDto.TransacType, "Failed", "Invalid amount");
                    bankDbContext.SaveChanges();
                    return BadRequest("Amount must be greater than zero");
                }

                if (creditDebitDto.Amount > 250000)
                {
                    SaveTransaction(creditDebitDto.AccNo, null, creditDebitDto.Amount, creditDebitDto.TransacType, "Failed", "Exceeds transaction limit");
                    bankDbContext.SaveChanges();
                    return BadRequest("Higher amount transaction alert. Please consult branch manager");
                }

                if (!(creditDebitDto.TransacType.Equals("Credit", StringComparison.OrdinalIgnoreCase) ||
                      creditDebitDto.TransacType.Equals("Debit", StringComparison.OrdinalIgnoreCase)))
                {
                    SaveTransaction(creditDebitDto.AccNo, null, creditDebitDto.Amount, creditDebitDto.TransacType, "Failed", "Invalid transaction type");
                    bankDbContext.SaveChanges();
                    return BadRequest("Transaction type should be either Credit or Debit");
                }

                string status = "Completed";
                if (creditDebitDto.TransacType.Equals("Credit", StringComparison.OrdinalIgnoreCase))
                {
                    account.Balance += creditDebitDto.Amount;
                }
                else if (creditDebitDto.TransacType.Equals("Debit", StringComparison.OrdinalIgnoreCase))
                {
                    if (account.Balance < creditDebitDto.Amount)
                    {
                        SaveTransaction(creditDebitDto.AccNo, null, creditDebitDto.Amount, "Debit", "Failed", "Insufficient balance");
                        bankDbContext.SaveChanges();
                        return BadRequest("Insufficient balance for debit transaction");
                    }
                    account.Balance -= creditDebitDto.Amount;
                }

                var transaction = new Transaction
                {
                    TransacType = creditDebitDto.TransacType,
                    Amount = creditDebitDto.Amount,
                    AccNo = creditDebitDto.AccNo,
                    ToAcc = null,
                    TimeStamps = DateTime.Now,
                    TransacStatus = status,
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
                {
                    return BadRequest("Enter the transaction details");
                }

                var fromAccount = bankDbContext.Accounts.Find(transferDto.FromAcc);
                var toAccount = bankDbContext.Accounts.Find(transferDto.ToAcc);

                if (fromAccount == null)
                {
                    SaveTransaction(transferDto.FromAcc, transferDto.ToAcc, transferDto.Amount, "Transfer", "Failed", "Source account not found");
                    bankDbContext.SaveChanges();
                    return NotFound("Source account not found");
                }
                if (toAccount == null)
                {
                    SaveTransaction(transferDto.FromAcc, transferDto.ToAcc, transferDto.Amount, "Transfer", "Failed", "Destination account not found");
                    bankDbContext.SaveChanges();
                    return NotFound("Destination account not found");
                }

                if (!fromAccount.AccountStatus.Equals("Active", StringComparison.OrdinalIgnoreCase))
                {
                    SaveTransaction(transferDto.FromAcc, transferDto.ToAcc, transferDto.Amount, "Transfer", "Failed", "Source account not active");
                    bankDbContext.SaveChanges();
                    return BadRequest("Transfer can be initiated only from active accounts");
                }

                if (toAccount.AccountStatus.Equals("Closed", StringComparison.OrdinalIgnoreCase))
                {
                    SaveTransaction(transferDto.FromAcc, transferDto.ToAcc, transferDto.Amount, "Transfer", "Failed", "Destination account is closed");
                    bankDbContext.SaveChanges();
                    return BadRequest("Cannot transfer to a closed account");
                }

                if (transferDto.FromAcc == transferDto.ToAcc)
                {
                    SaveTransaction(transferDto.FromAcc, transferDto.ToAcc, transferDto.Amount, "Transfer", "Failed", "Cannot transfer to same account");
                    bankDbContext.SaveChanges();
                    return BadRequest("Cannot transfer to the same account");
                }

                if (transferDto.Amount <= 0)
                {
                    SaveTransaction(transferDto.FromAcc, transferDto.ToAcc, transferDto.Amount, "Transfer", "Failed", "Invalid amount");
                    bankDbContext.SaveChanges();
                    return BadRequest("Amount must be greater than zero");
                }

                if (transferDto.Amount > 250000)
                {
                    SaveTransaction(transferDto.FromAcc, transferDto.ToAcc, transferDto.Amount, "Transfer", "Failed", "Exceeds transfer limit");
                    bankDbContext.SaveChanges();
                    return BadRequest("Transfer amount exceeds limit. Please consult branch manager");
                }

                if (fromAccount.Balance < transferDto.Amount)
                {
                    SaveTransaction(transferDto.FromAcc, transferDto.ToAcc, transferDto.Amount, "Transfer", "Failed", "Insufficient balance");
                    bankDbContext.SaveChanges();
                    return BadRequest("Insufficient balance in source account");
                }

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

                return Ok($"Transferred {transferDto.Amount} from {transferDto.FromAcc} to {transferDto.ToAcc} successfully");
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
                if (id < 1000000000)
                {
                    return BadRequest("Enter valid transaction ID");
                }
                var transaction = bankDbContext.Transactions.Find(id);

                if (transaction == null)
                {
                    return NotFound($"Transaction with ID {id} not found");
                }

                if (!transaction.TransacStatus.Equals("Failed", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest("Only failed transactions can be deleted");
                }

                bankDbContext.Transactions.Remove(transaction);
                bankDbContext.SaveChanges();

                return Ok($"Failed transaction with ID {id} has been deleted successfully");
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

                                   BranchName = b.BranchName,
                                   BranchAddress = b.Baddress
                               }).FirstOrDefault();

                if (account == null)
                {
                    return NotFound($"Account with {id} not found");
                }

                return Ok(account);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        //[HttpPost("CreateAccount")]
        //public IActionResult CreateAccount(AccountDto accountDto)
        //{
        //    try
        //    {
        //        if (accountDto == null)
        //        {
        //            return BadRequest("Enter correct data");
        //        }

        //        var user = bankDbContext.Users.Find(accountDto.UserId);
        //        if (user == null)
        //        {
        //            return NotFound("User not found");
        //        }

        //        var branch = bankDbContext.Branches
        //                                  .FirstOrDefault(b => b.IfscCode == accountDto.IfscCode);
        //        if (branch == null)
        //        {
        //            return NotFound("Branch not found");
        //        }

        //        Account acc = new Account
        //        {
        //            UserId = accountDto.UserId,
        //            AccType = accountDto.AccType,
        //            Balance = accountDto.Balance,
        //            DateOfJoining = DateTime.UtcNow,
        //            IfscCode = branch.IfscCode,
        //            AccountStatus = "Active"
        //        };

        //        bankDbContext.Accounts.Add(acc);
        //        bankDbContext.SaveChanges();

        //        return Ok("Account created successfully");
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(ex.Message);
        //    }
        //}

        [HttpPost("CreateAccount")]
        public IActionResult CreateAccount(AccountDto accountDto)
        {
            try
            {
                if (accountDto == null)
                {
                    return BadRequest("Enter correct data");
                }

                //var branchId = HttpContext.Session.GetInt32("BranchId");
                //if (branchId == null)
                //{
                //    return Unauthorized("Login required to create account");
                //}

                var branchIdClaim = User.Claims.FirstOrDefault(c => c.Type == "BranchId")?.Value;

                if (branchIdClaim == null)
                {
                    return Unauthorized("Login required to view users");
                }

                int branchId = int.Parse(branchIdClaim);

                var branch = bankDbContext.Branches.FirstOrDefault(b => b.BranchId == branchId);
                if (branch == null)
                {
                    return NotFound("Branch not found for logged-in staff");
                }

                var user = bankDbContext.Users.Find(accountDto.UserId);
                if (user == null)
                {
                    return NotFound("User not found");
                }

                Account acc = new Account
                {
                    UserId = accountDto.UserId,
                    AccType = accountDto.AccType,
                    Balance = accountDto.Balance,
                    DateOfJoining = DateTime.UtcNow,
                    IfscCode = branch.IfscCode,  //Manager's own branch IFSC
                    AccountStatus = "Active"
                };

                bankDbContext.Accounts.Add(acc);
                bankDbContext.SaveChanges();

                return Ok($"Account created successfully under branch {branch.BranchName} ({branch.IfscCode})");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

    }
}

//In Create Account, the account should be created with the same IFSC code of the branch of the logged in staff
//Get all Users should retrieve all users list of the same branch of Staff
//In transactions Credit can happen only if account status is not "closed"
//In transactions Debit must happen only if account status in "active"
//In transactions Transfer can happen only if AccNo's account status is active and ToAcc's account status is not "closed"