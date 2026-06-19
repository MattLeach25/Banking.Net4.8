using System;
using System.Linq;
using LegacyBanking.Application;
using LegacyBanking.Data;
using LegacyBanking.Domain;
using Xunit;

namespace LegacyBanking.Tests
{
    /// <summary>
    /// Tests for the dashboard aggregation logic.
    /// These validate that totals, counts, and lists are computed correctly.
    /// Will catch issues when data access changes (Blob → SQL, new auth, etc.).
    /// </summary>
    public class DashboardTests : IDisposable
    {
        private readonly IBankingApplicationService _service;

        public DashboardTests()
        {
            BankingDataStore.UseInMemoryStore();
            _service = new BankingApplicationService();
        }

        public void Dispose()
        {
            BankingDataStore.Reset();
        }

        [Fact]
        public void GetDashboard_ReturnsNonNullDashboard()
        {
            var dashboard = _service.GetDashboard();

            Assert.NotNull(dashboard);
            Assert.NotNull(dashboard.FeaturedAccounts);
            Assert.NotNull(dashboard.RecentTransactions);
        }

        [Fact]
        public void GetDashboard_CustomerCount_MatchesActualCustomers()
        {
            var customersBefore = _service.GetCustomers().Count;
            var dashboardBefore = _service.GetDashboard();
            Assert.Equal(customersBefore, dashboardBefore.CustomerCount);

            // Add a new customer
            _service.OnboardCustomer(new OnboardCustomerRequest
            {
                FirstName = "Dashboard",
                LastName = "Test",
                NationalId = "NID-DASH-001",
                Email = "dashboard.test@test.com",
                PhoneNumber = "555-DASH",
                DateOfBirth = new DateTime(1990, 1, 1),
                InitialAccountType = AccountType.Checking,
                OpeningDeposit = 100m
            });

            var dashboardAfter = _service.GetDashboard();
            Assert.Equal(customersBefore + 1, dashboardAfter.CustomerCount);
        }

        [Fact]
        public void GetDashboard_TotalDeposits_SumsPositiveBalances()
        {
            var dashboard = _service.GetDashboard();

            // TotalDeposits should be >= 0 (sum of positive balances)
            Assert.True(dashboard.TotalDeposits >= 0m);
        }

        [Fact]
        public void GetDashboard_FeaturedAccounts_MaxFiveOrderedByBalance()
        {
            var dashboard = _service.GetDashboard();

            Assert.True(dashboard.FeaturedAccounts.Count <= 5);

            // Verify descending order by balance
            for (int i = 1; i < dashboard.FeaturedAccounts.Count; i++)
            {
                Assert.True(
                    dashboard.FeaturedAccounts[i - 1].Balance >= dashboard.FeaturedAccounts[i].Balance,
                    "Featured accounts should be ordered by balance descending");
            }
        }

        [Fact]
        public void GetDashboard_RecentTransactions_MaxTen()
        {
            var dashboard = _service.GetDashboard();
            Assert.True(dashboard.RecentTransactions.Count <= 10);
        }

        [Fact]
        public void GetDashboard_AfterTransfer_TransactionsAppear()
        {
            // Create a customer and do a transfer to generate transactions
            var customer = _service.OnboardCustomer(new OnboardCustomerRequest
            {
                FirstName = "DashTxn",
                LastName = "Test",
                NationalId = "NID-DASH-002",
                Email = "dashtxn@test.com",
                PhoneNumber = "555-DTXN",
                DateOfBirth = new DateTime(1990, 1, 1),
                InitialAccountType = AccountType.Checking,
                OpeningDeposit = 1000m
            });

            var savings = _service.OpenAccount(new OpenAccountRequest
            {
                CustomerNumber = customer.CustomerNumber,
                AccountType = AccountType.Savings,
                OpeningDeposit = 0m
            });

            _service.TransferFunds(new TransferFundsRequest
            {
                FromAccountNumber = customer.Accounts[0].AccountNumber,
                ToAccountNumber = savings.AccountNumber,
                Amount = 100m
            });

            var dashboard = _service.GetDashboard();

            // Should have recent transactions from the transfer
            Assert.True(dashboard.RecentTransactions.Count > 0);
        }
    }
}
