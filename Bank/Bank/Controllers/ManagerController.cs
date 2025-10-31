using Bank.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;

namespace Bank.Controllers
{
    
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "BranchManager")]
    public class ManagerController : BaseController
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
        private bool VerifyPassword(string? inputPassword, string hashedPassword)
        {
            //return BCrypt.Net.BCrypt.Verify(inputPassword, hashedPassword);
            return inputPassword == hashedPassword;
            //return true;
        }

        [HttpPatch("UpdateLoginPassword")]
        public IActionResult UpdateLoginPassword(UpdatePassDto passDto)
        {
            try
            {
                var EmpId = GetEmpId();
                if (passDto == null)
                {
                    return BadRequest("Invalid request");
                }
                    
                var staff = bankDbContext.Staff.Find(EmpId);

                if (staff == null)
                {
                    return NotFound("Staff not found for this staff ID");
                }
                    
                if (!VerifyPassword(passDto.previousPassword, staff.EmpPass))
                {
                    return BadRequest("Your old password is incorrect");
                }
                    
                if (passDto.newPassword != passDto.confirmPassword)
                {
                    return BadRequest("New password and confirmation do not match");
                }
                    
                if (VerifyPassword(passDto.newPassword, staff.EmpPass))
                {
                    return BadRequest("New password cannot be the same as the old password");
                }
                    
                staff.EmpPass = passDto.newPassword;
                bankDbContext.SaveChanges();

                return Ok("Password updated successfully");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error updating password: {ex.Message}");
            }
        }
        [HttpGet("Users")]
        public IActionResult GetAllUsers()
        {
            try
            {
                var branchId = GetBranchId();
                if (branchId == null)
                    return Unauthorized("Branch information not present in token");

                // Fetch users who have at least one account in this branch and are not soft-deleted
                var users = (from u in bankDbContext.Users
                             join a in bankDbContext.Accounts on u.UserId equals a.UserId
                             join b in bankDbContext.Branches on a.IfscCode equals b.IfscCode
                             where b.BranchId == branchId && u.SoftDelete == true
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
                             }).Distinct().ToList();

                if (users.Count == 0)
                    return NotFound("No users found for this branch");

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
                // GLOBAL: Allow fetching any user by ID (not branch-scoped)
                var user = bankDbContext.Users
                    .Where(u => u.UserId == id && u.SoftDelete == true)
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

                // Validate uniqueness among active users
                if (!string.IsNullOrWhiteSpace(newUser.Mobile) && bankDbContext.Users.Any(u => u.Mobile == newUser.Mobile && u.SoftDelete == true))
                    return BadRequest("Mobile number already exists");

                if (!string.IsNullOrWhiteSpace(newUser.Email) && bankDbContext.Users.Any(u => u.Email == newUser.Email && u.SoftDelete == true))
                    return BadRequest("Email already exists");

                if (!string.IsNullOrWhiteSpace(newUser.PANCard) && bankDbContext.Users.Any(u => u.PANCard == newUser.PANCard && u.SoftDelete == true))
                    return BadRequest("PAN card already exists");

                if (!string.IsNullOrWhiteSpace(newUser.AadharCard) && bankDbContext.Users.Any(u => u.AadharCard == newUser.AadharCard && u.SoftDelete == true))
                    return BadRequest("Aadhar card already exists");

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
                    TransactionPassword = generatedPassword,
                    SoftDelete = true,
                    Role = "Customer"
                };
                bankDbContext.Users.Add(u);
                bankDbContext.SaveChanges();

                var response = new
                {
                    message = "User added successfully. Please change your password.",
                    userDetails = new
                    {
                        UserId = u.UserId,
                        UserName = u.Uname,
                        Email = u.Email,
                        Mobile = u.Mobile,
                        LoginPassword = u.LoginPassword,
                        TransactionPassword = u.TransactionPassword
                    }
                };

                return Ok(response);
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

                // GLOBAL: allow updating by id (no branch restriction)
                if (!string.IsNullOrEmpty(userUpdate.UserName)) user.Uname = userUpdate.UserName;
                if (!string.IsNullOrEmpty(userUpdate.Email))
                {
                    // ensure email uniqueness (excluding current user)
                    if (bankDbContext.Users.Any(x => x.Email == userUpdate.Email && x.UserId != id && x.SoftDelete == true))
                        return BadRequest("Email already in use by another user");
                    user.Email = userUpdate.Email;
                }
                if (!string.IsNullOrEmpty(userUpdate.UAddress)) user.Uaddress = userUpdate.UAddress;
                if (!string.IsNullOrEmpty(userUpdate.Mobile))
                {
                    if (bankDbContext.Users.Any(x => x.Mobile == userUpdate.Mobile && x.UserId != id && x.SoftDelete == true))
                        return BadRequest("Mobile already in use by another user");
                    user.Mobile = userUpdate.Mobile;
                }

                bankDbContext.SaveChanges();
                return Ok("User details updated successfully");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPatch("DeleteUser/{id}")]
        public IActionResult DeleteUser(int id)
        {
            try
            {
                var user = bankDbContext.Users.Find(id);
                if (user == null)
                {
                    return NotFound($"User with ID {id} not found");
                }
                    
                var branchId = GetBranchId();
                if (branchId == null)
                {
                    return Unauthorized("Branch information not present in token");
                }
                    
                var userHasAccountInBranch = bankDbContext.Accounts.Any(a => a.UserId == id && bankDbContext.Branches.Any(b => b.IfscCode == a.IfscCode && b.BranchId == branchId));
                if (!userHasAccountInBranch)
                {
                    return Unauthorized("You are not authorized to delete this user");
                }
                    
                user.SoftDelete = false;

                var accounts = bankDbContext.Accounts.Where(a => a.UserId == id).ToList();
                foreach (var acc in accounts)
                {
                    if (acc.Balance > 0)
                    {
                        // Create a debit transaction for the entire balance
                        SaveTransaction(acc.AccNo, null, acc.Balance, "Debit", "Completed", "Account closed due to user deletion");
                        acc.Balance = 0;
                    }

                    acc.AccountStatus = "Closed";
                }

                bankDbContext.SaveChanges();

                return Ok("User removed successfully and their accounts were blocked with balances debited");
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
                if (accNo == null || accNo< 1000000)
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
                {
                    return BadRequest("Enter transaction details");
                }
                    
                var account = bankDbContext.Accounts.Find(creditDebitDto.AccNo);
                if (account == null)
                {
                    //SaveTransaction(creditDebitDto.AccNo, null, creditDebitDto.Amount, creditDebitDto.TransacType, "Failed", "Invalid account");
                    //bankDbContext.SaveChanges();
                    return NotFound("Account not found");
                }

                if (creditDebitDto.Amount <= 0)
                {
                    //SaveTransaction(creditDebitDto.AccNo, null, creditDebitDto.Amount, creditDebitDto.TransacType, "Failed", "Invalid amount");
                    //bankDbContext.SaveChanges();
                    return BadRequest("Amount must be greater than zero");
                }

                // For Debit: account must be Active
                if (creditDebitDto.TransacType.Equals("Debit", StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.Equals(account.AccountStatus, "Active", StringComparison.OrdinalIgnoreCase))
                    {
                        SaveTransaction(creditDebitDto.AccNo, null, creditDebitDto.Amount, "Debit", "Failed", "Account not Active for debit");
                        bankDbContext.SaveChanges();
                        return BadRequest("Account must be Active to perform debit");
                    }

                    if (account.Balance < creditDebitDto.Amount)
                    {
                        SaveTransaction(creditDebitDto.AccNo, null, creditDebitDto.Amount, "Debit", "Failed", "Insufficient balance");
                        bankDbContext.SaveChanges();
                        return BadRequest("Insufficient balance");
                    }

                    account.Balance -= creditDebitDto.Amount;
                }
                else if (creditDebitDto.TransacType.Equals("Credit", StringComparison.OrdinalIgnoreCase))
                {
                    // For Credit: account must NOT be Closed
                    if (string.Equals(account.AccountStatus, "Closed", StringComparison.OrdinalIgnoreCase))
                    {
                        SaveTransaction(creditDebitDto.AccNo, null, creditDebitDto.Amount, "Credit", "Failed", "Account is Closed - cannot credit");
                        bankDbContext.SaveChanges();
                        return BadRequest("Cannot credit to a Closed account");
                    }

                    account.Balance += creditDebitDto.Amount;
                }
                else
                {
                    SaveTransaction(creditDebitDto.AccNo, null, creditDebitDto.Amount, creditDebitDto.TransacType, "Failed", "Invalid transaction type");
                    bankDbContext.SaveChanges();
                    return BadRequest("Invalid transaction type");
                }

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

                var response = new
                {
                    message = $"Transaction {creditDebitDto.TransacType} successful",
                    transactionDetails = new
                    {
                        TransactionId = transaction.TransacId,
                        AccountNo = transaction.AccNo,
                        TransactionType = transaction.TransacType,
                        Amount = transaction.Amount,
                        Timestamp = transaction.TimeStamps.ToString("dd-MM-yyyy HH:mm"),
                        Status = transaction.TransacStatus,
                        CurrentBalance = account.Balance
                    }
                };
                return Ok(response);
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
                    return BadRequest("Enter transaction details");
                }
                    
                var fromAccount = bankDbContext.Accounts.Find(transferDto.FromAcc);
                var toAccount = bankDbContext.Accounts.Find(transferDto.ToAcc);

                if (fromAccount == null || toAccount == null)
                {
                    //SaveTransaction(transferDto.FromAcc, transferDto.ToAcc, transferDto.Amount, "Transfer", "Failed", "Invalid account(s)");
                    //bankDbContext.SaveChanges();
                    return NotFound("Invalid account(s)");
                }

                if (transferDto.Amount <= 0)
                {
                    //SaveTransaction(transferDto.FromAcc, transferDto.ToAcc, transferDto.Amount, "Transfer", "Failed", "Invalid amount");
                    //bankDbContext.SaveChanges();
                    return BadRequest("Amount must be greater than zero");
                }

                if (transferDto.FromAcc == transferDto.ToAcc)
                {
                    SaveTransaction(transferDto.FromAcc, transferDto.ToAcc, transferDto.Amount, "Transfer", "Failed", "Cannot transfer to the same account");
                    bankDbContext.SaveChanges();
                    return BadRequest("Cannot transfer to the same account");
                }

                // FromAcc must be Active
                if (!string.Equals(fromAccount.AccountStatus, "Active", StringComparison.OrdinalIgnoreCase))
                {
                    SaveTransaction(transferDto.FromAcc, transferDto.ToAcc, transferDto.Amount, "Transfer", "Failed", "From account not Active");
                    bankDbContext.SaveChanges();
                    return BadRequest("From account must be Active");
                }

                // ToAcc must NOT be Closed
                if (string.Equals(toAccount.AccountStatus, "Closed", StringComparison.OrdinalIgnoreCase))
                {
                    SaveTransaction(transferDto.FromAcc, transferDto.ToAcc, transferDto.Amount, "Transfer", "Failed", "Receiver account Blocked");
                    bankDbContext.SaveChanges();
                    return BadRequest("Receiver account is Closed and cannot receive transfers");
                }

                if (fromAccount.Balance < transferDto.Amount)
                {
                    SaveTransaction(transferDto.FromAcc, transferDto.ToAcc, transferDto.Amount, "Transfer", "Failed", "Insufficient balance");
                    bankDbContext.SaveChanges();
                    return BadRequest("Insufficient balance");
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

                var response = new
                {
                    message = $"Transaction Transfer successful",
                    transactionDetails = new
                    {
                        TransactionId = transaction.TransacId,
                        AccountNo = transaction.AccNo,
                        ReceiverAccountNo = transaction.ToAcc,
                        TransactionType = transaction.TransacType,
                        Amount = transaction.Amount,
                        Timestamp = transaction.TimeStamps.ToString("dd-MM-yyyy HH:mm"),
                        Status = transaction.TransacStatus,
                        CurrentBalance = fromAccount.Balance
                    }
                };
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        //[HttpDelete("Transactions/{id}")]
        //public IActionResult DeleteTransaction(long id)
        //{
        //    try
        //    {
        //        var transaction = bankDbContext.Transactions.Find(id);

        //        if (transaction == null)
        //        {
        //            return NotFound($"Transaction {id} not found");
        //        }
                    
        //        if (!transaction.TransacStatus.Equals("Failed", StringComparison.OrdinalIgnoreCase))
        //        {
        //            return BadRequest("Only failed transactions can be deleted");
        //        }
                    
        //        bankDbContext.Transactions.Remove(transaction);
        //        bankDbContext.SaveChanges();

        //        return Ok("Failed transaction deleted successfully");
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(ex.Message);
        //    }
        //}

        //////////ACCOUNTS PART//////

        [HttpGet("Accounts/{id}")]
        public IActionResult GetAccountById(int id)
        {
            try
            {
                if (id < 1000000)
                {
                    return BadRequest("Provide valid id");
                }
                
                var account = (from a in bankDbContext.Accounts
                               join u in bankDbContext.Users on a.UserId equals u.UserId
                               join b in bankDbContext.Branches on a.IfscCode equals b.IfscCode
                               where (a.AccNo == id && u.SoftDelete == true)
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
                {
                    return NotFound($"Account with ID {id} not found");
                }
                    
                return Ok(account);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("AccountsByBranch")]
        public IActionResult AccountsByBranch()
        {
            try
            {
                var branchId = GetBranchId();
                var ifsc = bankDbContext.Branches.Where(b => b.BranchId == branchId).Select(b => b.IfscCode).FirstOrDefault();
                var account = bankDbContext.Accounts.Where(i => i.IfscCode == ifsc).ToList();
                return Ok(account);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Error getting accounts" });
            }
        }

        [HttpPost("CreateAccount")]
        public IActionResult CreateAccount(AccountDto accountDto)
        {
            try
            {
                if (accountDto == null)
                {
                    return BadRequest("Enter correct data");
                }
                    
                var user = bankDbContext.Users.Find(accountDto.UserId);

                if (user == null || user.SoftDelete == false)
                {
                    return NotFound("User not found");
                }

                // Determine branch IFSC for this manager's branch
                var branchId = GetBranchId();
                if (branchId == null)
                {
                    return Unauthorized("Branch information not present in token");
                }
                    
                var branch = bankDbContext.Branches.FirstOrDefault(b => b.BranchId == branchId);

                if (branch == null)
                {
                    return NotFound("Branch not found");
                }

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
                var response = new
                {
                    message = "Account created successfully.",
                    accountDetails = new
                    {
                        AccountNumber = acc.AccNo,
                        AccountType = acc.AccType,
                        UserId = acc.UserId,
                        BranchId = branchId,
                        IFSCCode = acc.IfscCode,
                        AccountBalance = acc.Balance,
                        AccountStatus = acc.AccountStatus,
                        DateOfCreation = acc.DateOfJoining.ToString("yyyy-MM-dd HH:mm")
                    }
                };

                return Ok(response);
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
                var branchId = GetBranchId();
                if (branchId == null)
                {
                    return Unauthorized("Branch information not present in token");
                }
                    
                var emps = bankDbContext.Staff
                    .Where(s => s.SoftDelete == true && s.BranchId == branchId)
                    .Select(s => new StaffResponseDto
                    {
                        EmpID = s.EmpId,
                        EmpName = s.EmpName,
                        EmpRole = s.EmpRole,
                        EmpMobile = s.EmpMobile,
                        EmpEmail = s.EmpEmail,
                        BranchId = s.BranchId
                    }).ToList();

                if (emps.Count == 0)
                {
                    return NotFound("No staff found in your branch");
                }
                    
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
                if(id<100000)
                {
                    return BadRequest("Provide valid id");
                }

                var emp = bankDbContext.Staff
                    .Where(s => s.EmpId == id && s.SoftDelete == true)
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
                {
                    return NotFound($"Staff with ID {id} not found");
                }
                    
                return Ok(emp);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("GetDeletedUsers")]
        public IActionResult GetDUsers()
        {
            try
            {
                var branchId = GetBranchId();
                if (branchId == null)
                {
                    return Unauthorized("Branch information not present in token");
                }
                    
                var dusers = (from u in bankDbContext.Users
                              join a in bankDbContext.Accounts on u.UserId equals a.UserId
                              join b in bankDbContext.Branches on a.IfscCode equals b.IfscCode
                              where u.SoftDelete == false && b.BranchId == branchId
                              select u).Distinct().ToList();

                if (dusers.Count == 0)
                {
                    return NotFound("No deleted Users in your branch");
                }
                return Ok(dusers);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("PermanantDeleteUser/{id}")]
        public IActionResult PostDUsers(int id)
        {
            try
            {
                var branchId = GetBranchId();
                if (branchId == null)
                {
                    return Unauthorized("Branch information not present in token");
                }
                    
                var dusers = (from u in bankDbContext.Users
                              join a in bankDbContext.Accounts on u.UserId equals a.UserId
                              join b in bankDbContext.Branches on a.IfscCode equals b.IfscCode
                              where u.UserId == id && u.SoftDelete == false && b.BranchId == branchId
                              select u).FirstOrDefault();

                if (dusers == null)
                {
                    return NotFound("No deleted user with the given ID in your branch");
                }

                bankDbContext.Users.Remove(dusers);
                bankDbContext.SaveChanges();
                return Ok("Permanently deleted the user from the branch");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}