using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Bank.Models;

public partial class BankDbContext : DbContext
{
    public BankDbContext()
    {
    }

    public BankDbContext(DbContextOptions<BankDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Account> Accounts { get; set; }

    public virtual DbSet<Branch> Branches { get; set; }

    public virtual DbSet<Staff> Staff { get; set; }

    public virtual DbSet<Transaction> Transactions { get; set; }

    public virtual DbSet<User> Users { get; set; }

    //protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    //    => optionsBuilder.UseSqlServer("Name=ConnectionStrings:myconn");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.AccNo).HasName("PK__Account__91CBCB53B0291693");

            entity.ToTable("Account");

            entity.Property(e => e.AccType)
                .HasMaxLength(30)
                .IsUnicode(false);
            entity.Property(e => e.AccountStatus)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasDefaultValue("Active");
            entity.Property(e => e.Balance).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.DateOfJoining).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.IfscCode)
                .HasMaxLength(12)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("IFSC_Code");

            entity.HasOne(d => d.IfscCodeNavigation).WithMany(p => p.Accounts)
                .HasPrincipalKey(p => p.IfscCode)
                .HasForeignKey(d => d.IfscCode)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Account__IFSC_Co__5441852A");

            entity.HasOne(d => d.User).WithMany(p => p.Accounts)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Account__UserId__5165187F");
        });

        modelBuilder.Entity<Branch>(entity =>
        {
            entity.HasKey(e => e.BranchId).HasName("PK__Branch__A1682FC5E000280B");

            entity.ToTable("Branch");

            entity.HasIndex(e => e.IfscCode, "UQ__Branch__00EF9F3F9038E189").IsUnique();

            entity.Property(e => e.Baddress)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("BAddress");
            entity.Property(e => e.BranchName)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.IfscCode)
                .HasMaxLength(12)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("IFSC_Code");
        });

        modelBuilder.Entity<Staff>(entity =>
        {
            entity.HasKey(e => e.EmpId).HasName("PK__Staff__AF2DBB998FAF2DD2");

            entity.HasIndex(e => e.EmpMobile, "UQ__Staff__38EF47AEB2474127").IsUnique();

            entity.HasIndex(e => e.EmpEmail, "UQ__Staff__74E4A3D6F1A10A04").IsUnique();

            entity.Property(e => e.EmpEmail)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.EmpMobile)
                .HasMaxLength(10)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.EmpName)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.EmpPass)
                .HasMaxLength(128)
                .IsUnicode(false);
            entity.Property(e => e.EmpRole)
                .HasMaxLength(20)
                .IsUnicode(false);

            entity.HasOne(d => d.Branch).WithMany(p => p.Staff)
                .HasForeignKey(d => d.BranchId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK__Staff__BranchId__4E88ABD4");
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(e => e.TransacId).HasName("PK__Transact__FDD99D7917AF9C87");

            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Comments)
                .HasMaxLength(200)
                .IsUnicode(false);
            entity.Property(e => e.TimeStamps).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.TransacStatus)
                .HasMaxLength(25)
                .IsUnicode(false);
            entity.Property(e => e.TransacType)
                .HasMaxLength(25)
                .IsUnicode(false);

            entity.HasOne(d => d.AccNoNavigation).WithMany(p => p.TransactionAccNoNavigations)
                .HasForeignKey(d => d.AccNo)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK__Transacti__AccNo__6B24EA82");

            entity.HasOne(d => d.ToAccNavigation).WithMany(p => p.TransactionToAccNavigations)
                .HasForeignKey(d => d.ToAcc)
                .HasConstraintName("FK__Transacti__ToAcc__6C190EBB");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__1788CC4C8ACAFAC3");

            entity.HasIndex(e => e.Mobile, "UQ__Users__6FAE0782F646E7A8").IsUnique();

            entity.HasIndex(e => e.Email, "UQ__Users__A9D10534A8EC6653").IsUnique();

            entity.Property(e => e.Email)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Gender)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.Mobile)
                .HasMaxLength(10)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.Uaddress)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("UAddress");
            entity.Property(e => e.Uname)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("UName");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
