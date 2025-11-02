using Bank.Models;
using Bank.Services;
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
        readonly PasswordService passwordService;

        public ManagerController(BankDbContext _bankDbContext, PasswordService _passwordService)
        {
            this.bankDbContext = _bankDbContext;
            this.passwordService = _passwordService;
        }


        [HttpGet("GetProfile")]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                var staffId = GetUserId();
                if (staffId == null)
                    return BadRequest(new { message = "Invalid staff ID" });

                var staff = await bankDbContext.Staff
                    .Include(s => s.Branch)
                    .Where(s => s.EmpId == staffId)
                    .Select(s => new StaffProfileDto
                    {
                        EmpId = s.EmpId,
                        EmpName = s.EmpName,
                        EmpRole = s.EmpRole,
                        EmpMobile = s.EmpMobile,
                        EmpEmail = s.EmpEmail,
                        BranchId = s.BranchId,
                        BranchName = s.Branch != null ? s.Branch.BranchName : string.Empty,
                        BranchAddress = s.Branch != null ? s.Branch.Baddress : string.Empty
                    })
                    .FirstOrDefaultAsync();

                if (staff == null)
                    return NotFound(new { message = $"Staff with ID {staffId} not found" });

                return Ok(staff);
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
                var EmpId = GetEmpId();
                if (passDto == null)
                    return BadRequest(new { message = "Invalid request" });

                var staff = await bankDbContext.Staff.FindAsync(EmpId);
                if (staff == null)
                    return NotFound(new { message = "Staff not found for this staff ID" });

                bool isPrevPass = passwordService.VerifyPassword(staff.EmpPass, passDto.previousPassword);
                if (!isPrevPass)
                    return BadRequest(new { message = "Your old password is incorrect" });

                if (passDto.newPassword != passDto.confirmPassword)
                    return BadRequest(new { message = "New password and confirmation do not match" });

                bool isNewPass = passwordService.VerifyPassword(staff.EmpPass, passDto.newPassword);
                if (isNewPass)
                    return BadRequest(new { message = "New password cannot be the same as the old password" });

                staff.EmpPass = passwordService.HashPassword(passDto.newPassword);
                await bankDbContext.SaveChangesAsync();

                return Ok(new { message = "Password updated successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = $"Error updating password: {ex.Message}" });
            }
        }

        [HttpGet("Users")]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                var branchId = GetBranchId();
                if (branchId == null)
                    return Unauthorized(new { message = "Branch information not present in token" });

                var users = await (from u in bankDbContext.Users
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
                                   }).Distinct().ToListAsync();

                if (users.Count == 0)
                    return NotFound(new { message = "No users found for this branch" });

                return Ok(users);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("Users/{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            try
            {
                var user = await bankDbContext.Users
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
                    .FirstOrDefaultAsync();

                if (user == null)
                    return NotFound(new { message = $"User with ID {id} not found" });

                return Ok(user);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("AddUser")]
        public async Task<IActionResult> AddNewUser([FromBody] UserCreationDto newUser)
        {
            try
            {
                if (newUser == null)
                    return BadRequest(new { message = "Invalid user data" });

                if (!string.IsNullOrWhiteSpace(newUser.Mobile) &&
                    await bankDbContext.Users.AnyAsync(u => u.Mobile == newUser.Mobile && u.SoftDelete == true))
                    return BadRequest(new { message = "Mobile number already exists" });

                if (!string.IsNullOrWhiteSpace(newUser.Email) &&
                    await bankDbContext.Users.AnyAsync(u => u.Email == newUser.Email && u.SoftDelete == true))
                    return BadRequest(new { message = "Email already exists" });

                if (!string.IsNullOrWhiteSpace(newUser.PANCard) &&
                    await bankDbContext.Users.AnyAsync(u => u.PANCard == newUser.PANCard && u.SoftDelete == true))
                    return BadRequest(new { message = "PAN card already exists" });

                if (!string.IsNullOrWhiteSpace(newUser.AadharCard) &&
                    await bankDbContext.Users.AnyAsync(u => u.AadharCard == newUser.AadharCard && u.SoftDelete == true))
                    return BadRequest(new { message = "Aadhar card already exists" });

                string generatedPassword = $"{newUser.UserName}@123";
                string hasedPassword = passwordService.HashPassword(generatedPassword);

                var u = new User
                {
                    Uname = newUser.UserName,
                    DoB = newUser.DoB,
                    Uaddress = newUser.UAddress,
                    Gender = newUser.Gender,
                    Mobile = newUser.Mobile,
                    Email = newUser.Email,
                    PANCard = newUser.PANCard,
                    AadharCard = newUser.AadharCard,
                    LoginPassword = hasedPassword,
                    TransactionPassword = hasedPassword,
                    SoftDelete = true,
                    Role = "Customer"
                };

                await bankDbContext.Users.AddAsync(u);
                await bankDbContext.SaveChangesAsync();

                var response = new
                {
                    message = "User added successfully. Please change your password.",
                    userDetails = new
                    {
                        UserId = u.UserId,
                        UserName = u.Uname,
                        Email = u.Email,
                        Mobile = u.Mobile,
                        LoginPassword = generatedPassword,
                        TransactionPassword = generatedPassword
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("UpdateUser/{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UserUpdateDto userUpdate)
        {
            try
            {
                if (userUpdate == null)
                    return BadRequest(new { message = "Invalid details" });

                var user = await bankDbContext.Users.FindAsync(id);
                if (user == null)
                    return NotFound(new { message = $"User with ID {id} not found" });

                if (!string.IsNullOrEmpty(userUpdate.UserName))
                    user.Uname = userUpdate.UserName;

                if (!string.IsNullOrEmpty(userUpdate.Email))
                {
                    if (await bankDbContext.Users.AnyAsync(x => x.Email == userUpdate.Email && x.UserId != id && x.SoftDelete == true))
                        return BadRequest(new { message = "Email already in use by another user" });
                    user.Email = userUpdate.Email;
                }

                if (!string.IsNullOrEmpty(userUpdate.UAddress))
                    user.Uaddress = userUpdate.UAddress;

                if (!string.IsNullOrEmpty(userUpdate.Mobile))
                {
                    if (await bankDbContext.Users.AnyAsync(x => x.Mobile == userUpdate.Mobile && x.UserId != id && x.SoftDelete == true))
                        return BadRequest(new { message = "Mobile already in use by another user" });
                    user.Mobile = userUpdate.Mobile;
                }

                await bankDbContext.SaveChangesAsync();
                return Ok(new { message = "User details updated successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPatch("DeleteUser/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            try
            {
                var user = await bankDbContext.Users.FindAsync(id);
                if (user == null)
                    return NotFound(new { message = $"User with ID {id} not found" });

                var branchId = GetBranchId();
                if (branchId == null)
                    return Unauthorized(new { message = "Branch information not present in token" });

                var userHasAccountInBranch = await bankDbContext.Accounts
                    .AnyAsync(a => a.UserId == id &&
                        bankDbContext.Branches.Any(b => b.IfscCode == a.IfscCode && b.BranchId == branchId));

                if (!userHasAccountInBranch)
                    return Unauthorized(new { message = "You are not authorized to delete this user" });

                user.SoftDelete = false;

                var accounts = await bankDbContext.Accounts.Where(a => a.UserId == id).ToListAsync();
                foreach (var acc in accounts)
                {
                    if (acc.Balance > 0)
                    {
                        await SaveTransactionAsync(acc.AccNo, null, acc.Balance, "Debit", "Completed", "Account closed due to user deletion");
                        acc.Balance = 0;
                    }

                    acc.AccountStatus = "Closed";
                }

                await bankDbContext.SaveChangesAsync();

                return Ok(new { message = "User removed successfully and their accounts were blocked with balances debited" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }


        /////////TRANSACTIONS PART///////

        private async Task SaveTransactionAsync(int? fromAcc, int? toAcc, decimal amount, string type, string status, string? comments)
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
            await bankDbContext.Transactions.AddAsync(transaction);
        }

        [HttpGet("Transactions")]
        public async Task<IActionResult> GetAllTransactions(int? accNo)
        {
            try
            {
                if (accNo == null || accNo < 1000000)
                    return BadRequest(new { message = "Enter correct account number" });

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

                return Ok(new
                {
                    message = "Transactions fetched successfully",
                    transactions
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("Transactions/{id}")]
        public async Task<IActionResult> GetTransactionById(long id)
        {
            try
            {
                if (id < 1000000000)
                {
                    return BadRequest(new { message = "Enter valid transaction ID" });
                }

                var trans = await bankDbContext.Transactions
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
                    .FirstOrDefaultAsync();

                if (trans == null)
                    return NotFound(new { message = $"Transaction with ID {id} not found" });

                return Ok(new
                {
                    message = "Transaction fetched successfully",
                    transaction = trans
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("Transactions/CreditOrDebit")]
        public async Task<IActionResult> CreditOrDebit([FromBody] TransactionCreditDebitDto creditDebitDto)
        {
            try
            {
                if (creditDebitDto == null)
                {
                    return BadRequest(new { message = "Enter transaction details" });
                }

                var account = await bankDbContext.Accounts.FindAsync(creditDebitDto.AccNo);
                if (account == null)
                {
                    return NotFound(new { message = "Account not found" });
                }

                if (creditDebitDto.Amount <= 0)
                {
                    return BadRequest(new { message = "Amount must be greater than zero" });
                }

                if (creditDebitDto.TransacType.Equals("Debit", StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.Equals(account.AccountStatus, "Active", StringComparison.OrdinalIgnoreCase))
                    {
                        await SaveTransactionAsync(creditDebitDto.AccNo, null, creditDebitDto.Amount, "Debit", "Failed", "Account not Active for debit");
                        await bankDbContext.SaveChangesAsync();
                        return BadRequest(new { message = "Account must be Active to perform debit" });
                    }

                    if (account.Balance < creditDebitDto.Amount)
                    {
                        await SaveTransactionAsync(creditDebitDto.AccNo, null, creditDebitDto.Amount, "Debit", "Failed", "Insufficient balance");
                        await bankDbContext.SaveChangesAsync();
                        return BadRequest(new { message = "Insufficient balance" });
                    }

                    account.Balance -= creditDebitDto.Amount;
                }
                else if (creditDebitDto.TransacType.Equals("Credit", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.Equals(account.AccountStatus, "Closed", StringComparison.OrdinalIgnoreCase))
                    {
                        await SaveTransactionAsync(creditDebitDto.AccNo, null, creditDebitDto.Amount, "Credit", "Failed", "Account is Closed - cannot credit");
                        await bankDbContext.SaveChangesAsync();
                        return BadRequest(new { message = "Cannot credit to a Closed account" });
                    }

                    account.Balance += creditDebitDto.Amount;
                }
                else
                {
                    await SaveTransactionAsync(creditDebitDto.AccNo, null, creditDebitDto.Amount, creditDebitDto.TransacType, "Failed", "Invalid transaction type");
                    await bankDbContext.SaveChangesAsync();
                    return BadRequest(new { message = "Invalid transaction type" });
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

                await bankDbContext.Transactions.AddAsync(transaction);
                await bankDbContext.SaveChangesAsync();

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
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("Transactions/Transfer")]
        public async Task<IActionResult> Transfer([FromBody] TransactionTransferDto transferDto)
        {
            try
            {
                if (transferDto == null)
                {
                    return BadRequest(new { message = "Enter transaction details" });
                }

                var fromAccount = await bankDbContext.Accounts.FindAsync(transferDto.FromAcc);
                var toAccount = await bankDbContext.Accounts.FindAsync(transferDto.ToAcc);

                if (fromAccount == null || toAccount == null)
                {
                    return NotFound(new { message = "Invalid account(s)" });
                }

                if (transferDto.Amount <= 0)
                {
                    return BadRequest(new { message = "Amount must be greater than zero" });
                }

                if (transferDto.FromAcc == transferDto.ToAcc)
                {
                    await SaveTransactionAsync(transferDto.FromAcc, transferDto.ToAcc, transferDto.Amount, "Transfer", "Failed", "Cannot transfer to the same account");
                    await bankDbContext.SaveChangesAsync();
                    return BadRequest(new { message = "Cannot transfer to the same account" });
                }

                if (!string.Equals(fromAccount.AccountStatus, "Active", StringComparison.OrdinalIgnoreCase))
                {
                    await SaveTransactionAsync(transferDto.FromAcc, transferDto.ToAcc, transferDto.Amount, "Transfer", "Failed", "From account not Active");
                    await bankDbContext.SaveChangesAsync();
                    return BadRequest(new { message = "From account must be Active" });
                }

                if (string.Equals(toAccount.AccountStatus, "Closed", StringComparison.OrdinalIgnoreCase))
                {
                    await SaveTransactionAsync(transferDto.FromAcc, transferDto.ToAcc, transferDto.Amount, "Transfer", "Failed", "Receiver account Blocked");
                    await bankDbContext.SaveChangesAsync();
                    return BadRequest(new { message = "Receiver account is Closed and cannot receive transfers" });
                }

                if (fromAccount.Balance < transferDto.Amount)
                {
                    await SaveTransactionAsync(transferDto.FromAcc, transferDto.ToAcc, transferDto.Amount, "Transfer", "Failed", "Insufficient balance");
                    await bankDbContext.SaveChangesAsync();
                    return BadRequest(new { message = "Insufficient balance" });
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

                await bankDbContext.Transactions.AddAsync(transaction);
                await bankDbContext.SaveChangesAsync();

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
                return BadRequest(new { message = ex.Message });
            }
        }


        //////////ACCOUNTS PART//////

        [HttpGet("Accounts/{id}")]
        public async Task<IActionResult> GetAccountById(int id)
        {
            try
            {
                if (id < 1000000)
                {
                    return BadRequest(new { message = "Provide valid id" });
                }

                var account = await (from a in bankDbContext.Accounts
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
                                     }).FirstOrDefaultAsync();

                if (account == null)
                {
                    return NotFound(new { message = $"Account with ID {id} not found" });
                }

                return Ok(new
                {
                    message = "Account fetched successfully",
                    account
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("AccountsByBranch")]
        public async Task<IActionResult> AccountsByBranch()
        {
            try
            {
                var branchId = GetBranchId();
                if (branchId == null)
                    return BadRequest(new { message = "Invalid branch ID" });

                var accounts = await (from a in bankDbContext.Accounts
                                      join u in bankDbContext.Users on a.UserId equals u.UserId
                                      join b in bankDbContext.Branches on a.IfscCode equals b.IfscCode
                                      where b.BranchId == branchId && u.SoftDelete == true
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
                                      }).ToListAsync();

                if (accounts == null || accounts.Count == 0)
                    return NotFound(new { message = "No accounts found for this branch" });

                return Ok(new
                {
                    message = "Accounts fetched successfully",
                    accounts
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("CreateAccount")]
        public async Task<IActionResult> CreateAccount([FromBody] AccountDto accountDto)
        {
            try
            {
                if (accountDto == null)
                {
                    return BadRequest(new { message = "Enter correct data" });
                }

                var user = await bankDbContext.Users.FindAsync(accountDto.UserId);

                if (user == null || user.SoftDelete == false)
                {
                    return NotFound(new { message = "User not found" });
                }

                var branchId = GetBranchId();
                if (branchId == null)
                {
                    return Unauthorized(new { message = "Branch information not present in token" });
                }

                var branch = await bankDbContext.Branches.FirstOrDefaultAsync(b => b.BranchId == branchId);

                if (branch == null)
                {
                    return NotFound(new { message = "Branch not found" });
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

                await bankDbContext.Accounts.AddAsync(acc);
                await bankDbContext.SaveChangesAsync();

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
                        DateOfCreation = acc.DateOfJoining.ToString("dd-MM-yyyy HH:mm")
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        [HttpPatch("UpdateAccountStatus/{id}")]
        public async Task<IActionResult> UpdateAccountStatus(int id, [FromBody] AccountUpdateDto statusDto)
        {
            try
            {
                if (statusDto == null || string.IsNullOrWhiteSpace(statusDto.AccountStatus))
                {
                    return BadRequest(new { message = "Enter valid account status" });
                }
                var account = await bankDbContext.Accounts.FindAsync(id);
                if (account == null)
                {
                    return NotFound(new { message = "Account not found" });
                }
                account.AccountStatus = statusDto.AccountStatus;
                await bankDbContext.SaveChangesAsync();
                return Ok(new { message = "Account status updated successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        ////////// STAFF PART ////////

        [HttpGet("GetAllStaff")]
        public IActionResult GetAllStaff()
        {
            try
            {
                var branchId = GetBranchId();
                if (branchId == null)
                    return Unauthorized(new { message = "Branch information not present in token" });

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
                    return NotFound(new { message = "No staff found in your branch" });

                return Ok(new
                {
                    message = "Staff list retrieved successfully",
                    staff = emps
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }


        [HttpGet("GetStaffByID/{id}")]
        public IActionResult GetStaffByID(int id)
        {
            try
            {
                if (id < 100000)
                    return BadRequest(new { message = "Provide valid staff ID" });

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
                    return NotFound(new { message = $"Staff with ID {id} not found" });

                return Ok(new
                {
                    message = "Staff details retrieved successfully",
                    staff = emp
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }


        [HttpGet("GetDeletedUsers")]
        public IActionResult GetDUsers()
        {
            try
            {
                var branchId = GetBranchId();
                if (branchId == null)
                    return Unauthorized(new { message = "Branch information not present in token" });

                var dusers = (from u in bankDbContext.Users
                              join a in bankDbContext.Accounts on u.UserId equals a.UserId
                              join b in bankDbContext.Branches on a.IfscCode equals b.IfscCode
                              where u.SoftDelete == false && b.BranchId == branchId
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

                if (dusers.Count == 0)
                    return NotFound(new { message = "No deleted users in your branch" });

                return Ok(new
                {
                    message = "Deleted users retrieved successfully",
                    users = dusers
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("PermanantDeleteUser/{id}")]
        public IActionResult PostDUsers(int id)
        {
            try
            {
                var branchId = GetBranchId();
                if (branchId == null)
                    return Unauthorized(new { message = "Branch information not present in token" });

                var dusers = (from u in bankDbContext.Users
                              join a in bankDbContext.Accounts on u.UserId equals a.UserId
                              join b in bankDbContext.Branches on a.IfscCode equals b.IfscCode
                              where u.UserId == id && u.SoftDelete == false && b.BranchId == branchId
                              select u).FirstOrDefault();

                if (dusers == null)
                    return NotFound(new { message = "No deleted user with the given ID in your branch" });

                bankDbContext.Users.Remove(dusers);
                bankDbContext.SaveChanges();

                return Ok(new { message = "User permanently deleted from the branch" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}