using System;
using System.Linq;
using LegacyBanking.Application;
using LegacyBanking.Data;
using LegacyBanking.Domain;
using Xunit;

namespace LegacyBanking.Tests
{
    /// <summary>
    /// End-to-end scenario tests that exercise multiple operations together.
    /// These simulate real user workflows and validate data consistency
    /// across the full lifecycle. These are the most important tests for
    /// catching regressions during migration — if these pass, the core
    /// banking operations are working correctly regardless of the
    /// underlying infrastructure (file, blob, SQL, auth changes).
    /// </summary>
    public class BankingWorkflowTests : IDisposable
    {
        private readonly IBankingApplicationService _service;

        public BankingWorkflowTests()
        {
            BankingDataStore.UseInMemoryStore();
            _service = new BankingApplicationService();
        }

        public void Dispose()
        {
            BankingDataStore.Reset();
        }

        [Fact]
        public void FullLifecycle_OnboardTransferPay_BalancesCorrect()
        {
            // Step 1: Onboard a customer with $2000 in checking
            var customer = _service.OnboardCustomer(new OnboardCustomerRequest
            {
                FirstName = "Lifecycle",
                LastName = "Test",
                NationalId = "NID-LIFE-001",
                Email = "lifecycle@test.com",
                PhoneNumber = "555-LIFE",
                DateOfBirth = new DateTime(1985, 6, 15),
                InitialAccountType = AccountType.Checking,
                OpeningDeposit = 2000m
            });

            var checkingNumber = customer.Accounts[0].AccountNumber;

            // Step 2: Open a savings account with $500
            var savings = _service.OpenAccount(new OpenAccountRequest
            {
                CustomerNumber = customer.CustomerNumber,
                AccountType = AccountType.Savings,
                OpeningDeposit = 500m
            });

            // Step 3: Transfer $300 from checking to savings
            var transferResult = _service.TransferFunds(new TransferFundsRequest
            {
                FromAccountNumber = checkingNumber,
                ToAccountNumber = savings.AccountNumber,
                Amount = 300m
            });
            Assert.True(transferResult.Success);

            // Step 4: Make a $150 payment from checking
            var paymentResult = _service.MakePayment(new PaymentRequest
            {
                AccountNumber = checkingNumber,
                Amount = 150m,
                MerchantName = "Grocery Store"
            });
            Assert.True(paymentResult.Success);

            // Step 5: Verify final balances
            var updated = _service.GetCustomer(customer.CustomerNumber);
            var checking = updated.Accounts.First(a => a.AccountNumber == checkingNumber);
            var savingsAfter = updated.Accounts.First(a => a.AccountNumber == savings.AccountNumber);

            Assert.Equal(1550m, checking.Balance);    // 2000 - 300 - 150
            Assert.Equal(800m, savingsAfter.Balance);  // 500 + 300

            // Step 6: Total money in system should be preserved
            var totalMoney = updated.Accounts.Sum(a => a.Balance);
            Assert.Equal(2350m, totalMoney); // 2000 + 500 - 150 (payment left the system)
        }

        [Fact]
        public void TransferThenPayment_InsufficientAfterTransfer_PaymentFails()
        {
            var customer = _service.OnboardCustomer(new OnboardCustomerRequest
            {
                FirstName = "Overdraft",
                LastName = "Test",
                NationalId = "NID-LIFE-002",
                Email = "overdraft@test.com",
                PhoneNumber = "555-OVRD",
                DateOfBirth = new DateTime(1990, 1, 1),
                InitialAccountType = AccountType.Checking,
                OpeningDeposit = 500m
            });

            var checkingNumber = customer.Accounts[0].AccountNumber;

            var savings = _service.OpenAccount(new OpenAccountRequest
            {
                CustomerNumber = customer.CustomerNumber,
                AccountType = AccountType.Savings,
                OpeningDeposit = 0m
            });

            // Transfer most of the balance out
            _service.TransferFunds(new TransferFundsRequest
            {
                FromAccountNumber = checkingNumber,
                ToAccountNumber = savings.AccountNumber,
                Amount = 450m
            });

            // Now try to pay more than remaining balance (50)
            var paymentResult = _service.MakePayment(new PaymentRequest
            {
                AccountNumber = checkingNumber,
                Amount = 100m,
                MerchantName = "Too Expensive"
            });

            Assert.False(paymentResult.Success);

            // Checking balance should still be 50
            var updated = _service.GetCustomer(customer.CustomerNumber);
            var checking = updated.Accounts.First(a => a.AccountNumber == checkingNumber);
            Assert.Equal(50m, checking.Balance);
        }

        [Fact]
        public void MultipleCustomers_IndependentBalances()
        {
            // Create two customers
            var customer1 = _service.OnboardCustomer(new OnboardCustomerRequest
            {
                FirstName = "Customer",
                LastName = "One",
                NationalId = "NID-LIFE-003",
                Email = "cust1@test.com",
                PhoneNumber = "555-C001",
                DateOfBirth = new DateTime(1990, 1, 1),
                InitialAccountType = AccountType.Checking,
                OpeningDeposit = 1000m
            });

            var customer2 = _service.OnboardCustomer(new OnboardCustomerRequest
            {
                FirstName = "Customer",
                LastName = "Two",
                NationalId = "NID-LIFE-004",
                Email = "cust2@test.com",
                PhoneNumber = "555-C002",
                DateOfBirth = new DateTime(1990, 1, 1),
                InitialAccountType = AccountType.Checking,
                OpeningDeposit = 2000m
            });

            // Payment from customer 1
            _service.MakePayment(new PaymentRequest
            {
                AccountNumber = customer1.Accounts[0].AccountNumber,
                Amount = 300m,
                MerchantName = "Store"
            });

            // Verify customer 2's balance is untouched
            var updated1 = _service.GetCustomer(customer1.CustomerNumber);
            var updated2 = _service.GetCustomer(customer2.CustomerNumber);

            Assert.Equal(700m, updated1.Accounts[0].Balance);   // 1000 - 300
            Assert.Equal(2000m, updated2.Accounts[0].Balance);  // unchanged
        }

        [Fact]
        public void CrossCustomerTransfer_BothCustomersUpdated()
        {
            var sender = _service.OnboardCustomer(new OnboardCustomerRequest
            {
                FirstName = "Sender",
                LastName = "Cross",
                NationalId = "NID-LIFE-005",
                Email = "sender@test.com",
                PhoneNumber = "555-SEND",
                DateOfBirth = new DateTime(1990, 1, 1),
                InitialAccountType = AccountType.Checking,
                OpeningDeposit = 1000m
            });

            var receiver = _service.OnboardCustomer(new OnboardCustomerRequest
            {
                FirstName = "Receiver",
                LastName = "Cross",
                NationalId = "NID-LIFE-006",
                Email = "receiver@test.com",
                PhoneNumber = "555-RECV",
                DateOfBirth = new DateTime(1990, 1, 1),
                InitialAccountType = AccountType.Checking,
                OpeningDeposit = 200m
            });

            var result = _service.TransferFunds(new TransferFundsRequest
            {
                FromAccountNumber = sender.Accounts[0].AccountNumber,
                ToAccountNumber = receiver.Accounts[0].AccountNumber,
                Amount = 400m
            });

            Assert.True(result.Success);

            var updatedSender = _service.GetCustomer(sender.CustomerNumber);
            var updatedReceiver = _service.GetCustomer(receiver.CustomerNumber);

            Assert.Equal(600m, updatedSender.Accounts[0].Balance);   // 1000 - 400
            Assert.Equal(600m, updatedReceiver.Accounts[0].Balance); // 200 + 400
        }
    }
}
