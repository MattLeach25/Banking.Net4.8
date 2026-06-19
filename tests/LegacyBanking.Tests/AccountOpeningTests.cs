using System;
using System.Linq;
using LegacyBanking.Application;
using LegacyBanking.Data;
using LegacyBanking.Domain;
using Xunit;

namespace LegacyBanking.Tests
{
    /// <summary>
    /// Tests for account opening logic.
    /// These validate account creation and initial deposit behavior
    /// regardless of data store changes (Blob Storage, SQL, etc.).
    /// </summary>
    public class AccountOpeningTests : IDisposable
    {
        private readonly IBankingApplicationService _service;

        public AccountOpeningTests()
        {
            BankingDataStore.UseInMemoryStore();
            _service = new BankingApplicationService();
        }

        public void Dispose()
        {
            BankingDataStore.Reset();
        }

        [Fact]
        public void OpenAccount_Checking_CreatesWithCorrectBalance()
        {
            var customer = CreateTestCustomer("AcctOpen", "001");

            var account = _service.OpenAccount(new OpenAccountRequest
            {
                CustomerNumber = customer.CustomerNumber,
                AccountType = AccountType.Checking,
                OpeningDeposit = 750m
            });

            Assert.NotNull(account);
            Assert.False(string.IsNullOrEmpty(account.AccountNumber));
            Assert.Equal("Checking", account.AccountType);
            Assert.Equal(750m, account.Balance);
            Assert.Equal("Active", account.Status);
        }

        [Fact]
        public void OpenAccount_Savings_CreatesWithCorrectBalance()
        {
            var customer = CreateTestCustomer("AcctOpen", "002");

            var account = _service.OpenAccount(new OpenAccountRequest
            {
                CustomerNumber = customer.CustomerNumber,
                AccountType = AccountType.Savings,
                OpeningDeposit = 1000m
            });

            Assert.Equal("Savings", account.AccountType);
            Assert.Equal(1000m, account.Balance);
        }

        [Fact]
        public void OpenAccount_MoneyMarket_CreatesWithCorrectBalance()
        {
            var customer = CreateTestCustomer("AcctOpen", "003");

            var account = _service.OpenAccount(new OpenAccountRequest
            {
                CustomerNumber = customer.CustomerNumber,
                AccountType = AccountType.MoneyMarket,
                OpeningDeposit = 5000m
            });

            Assert.Equal("MoneyMarket", account.AccountType);
            Assert.Equal(5000m, account.Balance);
        }

        [Fact]
        public void OpenAccount_ZeroDeposit_CreatesWithZeroBalance()
        {
            var customer = CreateTestCustomer("AcctOpen", "004");

            var account = _service.OpenAccount(new OpenAccountRequest
            {
                CustomerNumber = customer.CustomerNumber,
                AccountType = AccountType.Checking,
                OpeningDeposit = 0m
            });

            Assert.Equal(0m, account.Balance);
        }

        [Fact]
        public void OpenAccount_MultipleAccounts_AllTrackedUnderCustomer()
        {
            var customer = CreateTestCustomer("AcctOpen", "005");

            _service.OpenAccount(new OpenAccountRequest
            {
                CustomerNumber = customer.CustomerNumber,
                AccountType = AccountType.Checking,
                OpeningDeposit = 200m
            });

            _service.OpenAccount(new OpenAccountRequest
            {
                CustomerNumber = customer.CustomerNumber,
                AccountType = AccountType.Savings,
                OpeningDeposit = 300m
            });

            var updated = _service.GetCustomer(customer.CustomerNumber);

            // Original account from onboarding + 2 new = 3
            Assert.Equal(3, updated.Accounts.Count);
            Assert.Equal(200m + 300m + 100m, updated.Accounts.Sum(a => a.Balance));
        }

        [Fact]
        public void OpenAccount_InvalidCustomerNumber_ThrowsException()
        {
            Assert.Throws<InvalidOperationException>(() =>
                _service.OpenAccount(new OpenAccountRequest
                {
                    CustomerNumber = "CUST-INVALID",
                    AccountType = AccountType.Checking,
                    OpeningDeposit = 100m
                }));
        }

        [Fact]
        public void OpenAccount_NullRequest_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _service.OpenAccount(null));
        }

        private CustomerProfileDto CreateTestCustomer(string prefix, string suffix)
        {
            return _service.OnboardCustomer(new OnboardCustomerRequest
            {
                FirstName = prefix,
                LastName = suffix,
                NationalId = $"NID-{prefix}-{suffix}",
                Email = $"{prefix}.{suffix}@test.com",
                PhoneNumber = $"555-{suffix}",
                DateOfBirth = new DateTime(1990, 1, 1),
                InitialAccountType = AccountType.Checking,
                OpeningDeposit = 100m
            });
        }
    }
}
