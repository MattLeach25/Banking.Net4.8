using System;
using System.Linq;
using LegacyBanking.Application;
using LegacyBanking.Data;
using LegacyBanking.Domain;
using Xunit;

namespace LegacyBanking.Tests
{
    /// <summary>
    /// Tests for fund transfer logic.
    /// These validate that money moves correctly between accounts,
    /// balances update accurately, and edge cases are handled.
    /// Critical for: SQL migration (data integrity), Blob→SQL swap,
    /// connection string changes, Managed Identity auth.
    /// </summary>
    public class TransferFundsTests : IDisposable
    {
        private readonly IBankingApplicationService _service;

        public TransferFundsTests()
        {
            BankingDataStore.UseInMemoryStore();
            _service = new BankingApplicationService();
        }

        public void Dispose()
        {
            BankingDataStore.Reset();
        }

        [Fact]
        public void TransferFunds_ValidTransfer_DebitsCreditsBothAccounts()
        {
            var customer = CreateCustomerWithAccounts("Transfer", "001", 1000m, 500m);
            var checkingNumber = customer.Accounts[0].AccountNumber;
            var savingsNumber = customer.Accounts[1].AccountNumber;

            var result = _service.TransferFunds(new TransferFundsRequest
            {
                FromAccountNumber = checkingNumber,
                ToAccountNumber = savingsNumber,
                Amount = 200m
            });

            Assert.True(result.Success);
            Assert.False(string.IsNullOrEmpty(result.ReferenceNumber));

            var updated = _service.GetCustomer(customer.CustomerNumber);
            var checking = updated.Accounts.First(a => a.AccountNumber == checkingNumber);
            var savings = updated.Accounts.First(a => a.AccountNumber == savingsNumber);

            Assert.Equal(800m, checking.Balance);   // 1000 - 200
            Assert.Equal(700m, savings.Balance);     // 500 + 200
        }

        [Fact]
        public void TransferFunds_ExactBalance_TransfersAll()
        {
            var customer = CreateCustomerWithAccounts("Transfer", "002", 300m, 0m);
            var fromNumber = customer.Accounts[0].AccountNumber;
            var toNumber = customer.Accounts[1].AccountNumber;

            var result = _service.TransferFunds(new TransferFundsRequest
            {
                FromAccountNumber = fromNumber,
                ToAccountNumber = toNumber,
                Amount = 300m
            });

            Assert.True(result.Success);

            var updated = _service.GetCustomer(customer.CustomerNumber);
            var from = updated.Accounts.First(a => a.AccountNumber == fromNumber);
            var to = updated.Accounts.First(a => a.AccountNumber == toNumber);

            Assert.Equal(0m, from.Balance);
            Assert.Equal(300m, to.Balance);
        }

        [Fact]
        public void TransferFunds_InsufficientBalance_FailsWithMessage()
        {
            var customer = CreateCustomerWithAccounts("Transfer", "003", 100m, 50m);
            var fromNumber = customer.Accounts[0].AccountNumber;
            var toNumber = customer.Accounts[1].AccountNumber;

            var result = _service.TransferFunds(new TransferFundsRequest
            {
                FromAccountNumber = fromNumber,
                ToAccountNumber = toNumber,
                Amount = 9999m
            });

            Assert.False(result.Success);
            Assert.Contains("Insufficient", result.Message, StringComparison.OrdinalIgnoreCase);

            // Verify balances unchanged
            var updated = _service.GetCustomer(customer.CustomerNumber);
            var from = updated.Accounts.First(a => a.AccountNumber == fromNumber);
            var to = updated.Accounts.First(a => a.AccountNumber == toNumber);

            Assert.Equal(100m, from.Balance);
            Assert.Equal(50m, to.Balance);
        }

        [Fact]
        public void TransferFunds_SameAccount_FailsWithMessage()
        {
            var customer = CreateCustomerWithAccounts("Transfer", "004", 500m, 100m);
            var accountNumber = customer.Accounts[0].AccountNumber;

            var result = _service.TransferFunds(new TransferFundsRequest
            {
                FromAccountNumber = accountNumber,
                ToAccountNumber = accountNumber,
                Amount = 100m
            });

            Assert.False(result.Success);
        }

        [Fact]
        public void TransferFunds_NonExistentFromAccount_FailsWithMessage()
        {
            var customer = CreateCustomerWithAccounts("Transfer", "005", 500m, 100m);

            var result = _service.TransferFunds(new TransferFundsRequest
            {
                FromAccountNumber = "ACCT-DOESNOTEXIST",
                ToAccountNumber = customer.Accounts[0].AccountNumber,
                Amount = 100m
            });

            Assert.False(result.Success);
        }

        [Fact]
        public void TransferFunds_NonExistentToAccount_FailsWithMessage()
        {
            var customer = CreateCustomerWithAccounts("Transfer", "006", 500m, 100m);

            var result = _service.TransferFunds(new TransferFundsRequest
            {
                FromAccountNumber = customer.Accounts[0].AccountNumber,
                ToAccountNumber = "ACCT-DOESNOTEXIST",
                Amount = 100m
            });

            Assert.False(result.Success);
        }

        [Fact]
        public void TransferFunds_MultipleTransfers_BalancesAccumulate()
        {
            var customer = CreateCustomerWithAccounts("Transfer", "007", 1000m, 0m);
            var fromNumber = customer.Accounts[0].AccountNumber;
            var toNumber = customer.Accounts[1].AccountNumber;

            _service.TransferFunds(new TransferFundsRequest
            {
                FromAccountNumber = fromNumber,
                ToAccountNumber = toNumber,
                Amount = 100m
            });

            _service.TransferFunds(new TransferFundsRequest
            {
                FromAccountNumber = fromNumber,
                ToAccountNumber = toNumber,
                Amount = 200m
            });

            _service.TransferFunds(new TransferFundsRequest
            {
                FromAccountNumber = fromNumber,
                ToAccountNumber = toNumber,
                Amount = 50m
            });

            var updated = _service.GetCustomer(customer.CustomerNumber);
            var from = updated.Accounts.First(a => a.AccountNumber == fromNumber);
            var to = updated.Accounts.First(a => a.AccountNumber == toNumber);

            Assert.Equal(650m, from.Balance);   // 1000 - 100 - 200 - 50
            Assert.Equal(350m, to.Balance);     // 0 + 100 + 200 + 50
        }

        [Fact]
        public void TransferFunds_NullRequest_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _service.TransferFunds(null));
        }

        private CustomerProfileDto CreateCustomerWithAccounts(string prefix, string suffix, decimal checkingBalance, decimal savingsBalance)
        {
            var customer = _service.OnboardCustomer(new OnboardCustomerRequest
            {
                FirstName = prefix,
                LastName = suffix,
                NationalId = $"NID-{prefix}-{suffix}",
                Email = $"{prefix}.{suffix}@test.com",
                PhoneNumber = $"555-{suffix}",
                DateOfBirth = new DateTime(1990, 1, 1),
                InitialAccountType = AccountType.Checking,
                OpeningDeposit = checkingBalance
            });

            _service.OpenAccount(new OpenAccountRequest
            {
                CustomerNumber = customer.CustomerNumber,
                AccountType = AccountType.Savings,
                OpeningDeposit = savingsBalance
            });

            return _service.GetCustomer(customer.CustomerNumber);
        }
    }
}
