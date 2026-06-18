using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using LegacyBanking.Domain;

namespace LegacyBanking.Application
{
    public sealed class BankingDashboardDto
    {
        public decimal TotalDeposits { get; set; }
        public decimal TotalWithdrawals { get; set; }
        public int CustomerCount { get; set; }
        public int AccountCount { get; set; }
        public IReadOnlyList<AccountDto> FeaturedAccounts { get; set; } = Array.Empty<AccountDto>();
        public IReadOnlyList<TransactionDto> RecentTransactions { get; set; } = Array.Empty<TransactionDto>();
    }

    public sealed class CustomerProfileDto
    {
        public int CustomerId { get; set; }
        public string CustomerNumber { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string Status { get; set; }
        public DateTime DateOfBirth { get; set; }
        public DateTime CreatedOn { get; set; }
        public IReadOnlyList<AccountDto> Accounts { get; set; } = Array.Empty<AccountDto>();
    }

    public sealed class AccountDto
    {
        public int AccountId { get; set; }
        public string AccountNumber { get; set; }
        public string CustomerNumber { get; set; }
        public string CustomerName { get; set; }
        public string AccountType { get; set; }
        public string Status { get; set; }
        public decimal Balance { get; set; }
        public DateTime OpenedOn { get; set; }
    }

    public sealed class TransactionDto
    {
        public string ReferenceNumber { get; set; }
        public string AccountNumber { get; set; }
        public string Description { get; set; }
        public string TransactionType { get; set; }
        public decimal Amount { get; set; }
        public decimal BalanceAfter { get; set; }
        public DateTime PostedOn { get; set; }
    }

    public sealed class OnboardCustomerRequest
    {
        [Required]
        [StringLength(50)]
        public string FirstName { get; set; }

        [Required]
        [StringLength(50)]
        public string LastName { get; set; }

        [Required]
        [StringLength(25)]
        public string NationalId { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(120)]
        public string Email { get; set; }

        [Required]
        [StringLength(25)]
        public string PhoneNumber { get; set; }

        [Required]
        public DateTime DateOfBirth { get; set; }

        [Required]
        public AccountType InitialAccountType { get; set; }

        [Range(0, 10000000)]
        public decimal OpeningDeposit { get; set; }
    }

    public sealed class OpenAccountRequest
    {
        [Required]
        [StringLength(20)]
        public string CustomerNumber { get; set; }

        [Required]
        public AccountType AccountType { get; set; }

        [Range(0, 10000000)]
        public decimal OpeningDeposit { get; set; }
    }

    public sealed class TransferFundsRequest
    {
        [Required]
        [StringLength(20)]
        public string FromAccountNumber { get; set; }

        [Required]
        [StringLength(20)]
        public string ToAccountNumber { get; set; }

        [Range(0.01, 10000000)]
        public decimal Amount { get; set; }
    }

    public sealed class PaymentRequest
    {
        [Required]
        [StringLength(20)]
        public string AccountNumber { get; set; }

        [Required]
        [StringLength(100)]
        public string MerchantName { get; set; }

        [Range(0.01, 10000000)]
        public decimal Amount { get; set; }
    }

    public sealed class BankingOperationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string ReferenceNumber { get; set; }
        public decimal? BalanceAfter { get; set; }
    }
}
