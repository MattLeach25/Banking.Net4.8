using System;
using System.Linq;
using LegacyBanking.Application;
using LegacyBanking.Data;
using LegacyBanking.Domain;
using Xunit;

namespace LegacyBanking.Tests
{
    /// <summary>
    /// Tests for customer onboarding logic.
    /// These validate that customer creation works correctly regardless of
    /// the underlying data store (local file, Azure Blob, SQL Server).
    /// </summary>
    public class CustomerOnboardingTests : IDisposable
    {
        private readonly IBankingApplicationService _service;

        public CustomerOnboardingTests()
        {
            BankingDataStore.UseInMemoryStore();
            _service = new BankingApplicationService();
        }

        public void Dispose()
        {
            BankingDataStore.Reset();
        }

        [Fact]
        public void OnboardCustomer_ValidRequest_CreatesCustomerWithAccount()
        {
            var request = new OnboardCustomerRequest
            {
                FirstName = "Alice",
                LastName = "Johnson",
                NationalId = "NID-ONB-001",
                Email = "alice.onboard@test.com",
                PhoneNumber = "555-1001",
                DateOfBirth = new DateTime(1990, 3, 15),
                InitialAccountType = AccountType.Checking,
                OpeningDeposit = 500m
            };

            var customer = _service.OnboardCustomer(request);

            Assert.NotNull(customer);
            Assert.False(string.IsNullOrEmpty(customer.CustomerNumber));
            Assert.Equal("Alice Johnson", customer.FullName);
            Assert.Equal("alice.onboard@test.com", customer.Email);
            Assert.Equal("555-1001", customer.PhoneNumber);
            Assert.Equal("Active", customer.Status);
            Assert.Single(customer.Accounts);
            Assert.Equal("Checking", customer.Accounts[0].AccountType);
            Assert.Equal(500m, customer.Accounts[0].Balance);
        }

        [Fact]
        public void OnboardCustomer_ZeroDeposit_CreatesAccountWithZeroBalance()
        {
            var request = new OnboardCustomerRequest
            {
                FirstName = "Bob",
                LastName = "Zero",
                NationalId = "NID-ONB-002",
                Email = "bob.zero@test.com",
                PhoneNumber = "555-1002",
                DateOfBirth = new DateTime(1985, 7, 20),
                InitialAccountType = AccountType.Savings,
                OpeningDeposit = 0m
            };

            var customer = _service.OnboardCustomer(request);

            Assert.Single(customer.Accounts);
            Assert.Equal("Savings", customer.Accounts[0].AccountType);
            Assert.Equal(0m, customer.Accounts[0].Balance);
        }

        [Fact]
        public void OnboardCustomer_NullRequest_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _service.OnboardCustomer(null));
        }

        [Fact]
        public void GetCustomer_ExistingCustomer_ReturnsCorrectProfile()
        {
            var created = _service.OnboardCustomer(new OnboardCustomerRequest
            {
                FirstName = "Carol",
                LastName = "Lookup",
                NationalId = "NID-ONB-003",
                Email = "carol.lookup@test.com",
                PhoneNumber = "555-1003",
                DateOfBirth = new DateTime(1992, 11, 5),
                InitialAccountType = AccountType.Checking,
                OpeningDeposit = 100m
            });

            var retrieved = _service.GetCustomer(created.CustomerNumber);

            Assert.Equal(created.CustomerNumber, retrieved.CustomerNumber);
            Assert.Equal("Carol Lookup", retrieved.FullName);
            Assert.Equal("carol.lookup@test.com", retrieved.Email);
        }

        [Fact]
        public void GetCustomer_NonExistentCustomer_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => _service.GetCustomer("CUST-DOESNOTEXIST"));
        }

        [Fact]
        public void GetCustomers_ReturnsNonEmptyList()
        {
            // Ensure at least one customer exists
            _service.OnboardCustomer(new OnboardCustomerRequest
            {
                FirstName = "ListTest",
                LastName = "Customer",
                NationalId = "NID-ONB-004",
                Email = "listtest@test.com",
                PhoneNumber = "555-1004",
                DateOfBirth = new DateTime(1988, 1, 1),
                InitialAccountType = AccountType.Checking,
                OpeningDeposit = 100m
            });

            var customers = _service.GetCustomers();

            Assert.NotNull(customers);
            Assert.True(customers.Count > 0);
        }
    }
}
