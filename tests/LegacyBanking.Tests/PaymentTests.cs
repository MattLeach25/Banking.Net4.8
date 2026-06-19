using System;
using System.Linq;
using LegacyBanking.Application;
using LegacyBanking.Data;
using LegacyBanking.Domain;
using Xunit;

namespace LegacyBanking.Tests
{
    /// <summary>
    /// Tests for payment logic.
    /// These validate that payments debit correctly, handle insufficient
    /// funds, and produce reference numbers. Will catch issues when
    /// switching from Blob Storage to SQL Server.
    /// </summary>
    public class PaymentTests : IDisposable
    {
        private readonly IBankingApplicationService _service;

        public PaymentTests()
        {
            BankingDataStore.UseInMemoryStore();
            _service = new BankingApplicationService();
        }

        public void Dispose()
        {
            BankingDataStore.Reset();
        }

        [Fact]
        public void MakePayment_ValidPayment_DebitsAccountCorrectly()
        {
            var customer = CreateTestCustomer("Payment", "001", 1000m);
            var accountNumber = customer.Accounts[0].AccountNumber;

            var result = _service.MakePayment(new PaymentRequest
            {
                AccountNumber = accountNumber,
                Amount = 150m,
                MerchantName = "Electric Company"
            });

            Assert.True(result.Success);
            Assert.False(string.IsNullOrEmpty(result.ReferenceNumber));

            var updated = _service.GetCustomer(customer.CustomerNumber);
            Assert.Equal(850m, updated.Accounts[0].Balance); // 1000 - 150
        }

        [Fact]
        public void MakePayment_ExactBalance_PaysAll()
        {
            var customer = CreateTestCustomer("Payment", "002", 200m);
            var accountNumber = customer.Accounts[0].AccountNumber;

            var result = _service.MakePayment(new PaymentRequest
            {
                AccountNumber = accountNumber,
                Amount = 200m,
                MerchantName = "Full Payment Corp"
            });

            Assert.True(result.Success);

            var updated = _service.GetCustomer(customer.CustomerNumber);
            Assert.Equal(0m, updated.Accounts[0].Balance);
        }

        [Fact]
        public void MakePayment_InsufficientFunds_FailsWithMessage()
        {
            var customer = CreateTestCustomer("Payment", "003", 50m);
            var accountNumber = customer.Accounts[0].AccountNumber;

            var result = _service.MakePayment(new PaymentRequest
            {
                AccountNumber = accountNumber,
                Amount = 9999m,
                MerchantName = "Expensive Store"
            });

            Assert.False(result.Success);
            Assert.Contains("Insufficient", result.Message, StringComparison.OrdinalIgnoreCase);

            // Balance unchanged
            var updated = _service.GetCustomer(customer.CustomerNumber);
            Assert.Equal(50m, updated.Accounts[0].Balance);
        }

        [Fact]
        public void MakePayment_NonExistentAccount_FailsWithMessage()
        {
            var result = _service.MakePayment(new PaymentRequest
            {
                AccountNumber = "ACCT-DOESNOTEXIST",
                Amount = 100m,
                MerchantName = "Ghost Merchant"
            });

            Assert.False(result.Success);
        }

        [Fact]
        public void MakePayment_MultiplePayments_BalanceDecreasesCorrectly()
        {
            var customer = CreateTestCustomer("Payment", "004", 1000m);
            var accountNumber = customer.Accounts[0].AccountNumber;

            _service.MakePayment(new PaymentRequest
            {
                AccountNumber = accountNumber,
                Amount = 100m,
                MerchantName = "Store A"
            });

            _service.MakePayment(new PaymentRequest
            {
                AccountNumber = accountNumber,
                Amount = 250m,
                MerchantName = "Store B"
            });

            _service.MakePayment(new PaymentRequest
            {
                AccountNumber = accountNumber,
                Amount = 75m,
                MerchantName = "Store C"
            });

            var updated = _service.GetCustomer(customer.CustomerNumber);
            Assert.Equal(575m, updated.Accounts[0].Balance); // 1000 - 100 - 250 - 75
        }

        [Fact]
        public void MakePayment_NullRequest_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _service.MakePayment(null));
        }

        private CustomerProfileDto CreateTestCustomer(string prefix, string suffix, decimal openingDeposit)
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
                OpeningDeposit = openingDeposit
            });
        }
    }
}
