using System;
using System.Collections.Generic;

namespace LegacyBanking.Domain
{
    public enum CustomerStatus
    {
        Prospect = 0,
        Active = 1,
        Suspended = 2,
        Closed = 3
    }

    public enum AccountType
    {
        Checking = 0,
        Savings = 1,
        MoneyMarket = 2,
        Loan = 3
    }

    public enum AccountStatus
    {
        Active = 0,
        Frozen = 1,
        Closed = 2
    }

    public enum TransactionType
    {
        OpeningDeposit = 0,
        Deposit = 1,
        Withdrawal = 2,
        TransferIn = 3,
        TransferOut = 4,
        Payment = 5
    }

    public sealed class Customer
    {
        public int CustomerId { get; set; }
        public string CustomerNumber { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string NationalId { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime DateOfBirth { get; set; }
        public CustomerStatus Status { get; set; }
        public DateTime CreatedOn { get; set; }
        public ICollection<Account> Accounts { get; set; } = new HashSet<Account>();
    }

    public sealed class Account
    {
        public int AccountId { get; set; }
        public string AccountNumber { get; set; }
        public int CustomerId { get; set; }
        public Customer Customer { get; set; }
        public AccountType AccountType { get; set; }
        public AccountStatus Status { get; set; }
        public decimal Balance { get; set; }
        public DateTime OpenedOn { get; set; }
        public ICollection<Transaction> Transactions { get; set; } = new HashSet<Transaction>();
    }

    public sealed class Transaction
    {
        public int TransactionId { get; set; }
        public int AccountId { get; set; }
        public Account Account { get; set; }
        public DateTime PostedOn { get; set; }
        public TransactionType TransactionType { get; set; }
        public decimal Amount { get; set; }
        public decimal BalanceAfter { get; set; }
        public string Description { get; set; }
        public string ReferenceNumber { get; set; }
        public int? CounterpartyAccountId { get; set; }
    }
}
