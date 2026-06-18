using System;
using System.Collections.Generic;
using System.Linq;
using LegacyBanking.Data;
using LegacyBanking.Domain;

namespace LegacyBanking.Application
{
    public interface IBankingApplicationService
    {
        BankingDashboardDto GetDashboard();
        IReadOnlyList<CustomerProfileDto> GetCustomers();
        CustomerProfileDto GetCustomer(string customerNumber);
        AccountDto OpenAccount(OpenAccountRequest request);
        CustomerProfileDto OnboardCustomer(OnboardCustomerRequest request);
        BankingOperationResult TransferFunds(TransferFundsRequest request);
        BankingOperationResult MakePayment(PaymentRequest request);
    }

    public sealed class BankingApplicationService : IBankingApplicationService
    {
        private readonly BankingRepository repository;

        public BankingApplicationService() : this(new BankingRepository())
        {
        }

        public BankingApplicationService(BankingRepository repository)
        {
            this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public BankingDashboardDto GetDashboard()
        {
            var customers = repository.GetCustomers();
            var accounts = customers.SelectMany(customer => customer.Accounts).ToList();
            var recentTransactions = repository.GetRecentTransactions().Select(MapTransaction).ToList();

            return new BankingDashboardDto
            {
                CustomerCount = customers.Count,
                AccountCount = accounts.Count,
                TotalDeposits = accounts.Where(account => account.Balance > 0m).Sum(account => account.Balance),
                TotalWithdrawals = recentTransactions.Where(transaction => transaction.TransactionType == TransactionType.Withdrawal.ToString() || transaction.TransactionType == TransactionType.Payment.ToString() || transaction.TransactionType == TransactionType.TransferOut.ToString()).Sum(transaction => transaction.Amount),
                FeaturedAccounts = accounts.OrderByDescending(account => account.Balance).Take(5).Select(account => MapAccount(account)).ToList(),
                RecentTransactions = recentTransactions.Take(10).ToList()
            };
        }

        public IReadOnlyList<CustomerProfileDto> GetCustomers()
        {
            return repository.GetCustomers().Select(MapCustomerProfile).ToList();
        }

        public CustomerProfileDto GetCustomer(string customerNumber)
        {
            var customer = repository.GetCustomerByNumber(customerNumber);
            if (customer == null)
            {
                throw new InvalidOperationException("Customer not found.");
            }

            return MapCustomerProfile(customer);
        }

        public AccountDto OpenAccount(OpenAccountRequest request)
        {
            Validate(request);
            var account = repository.OpenAccount(request.CustomerNumber, request.AccountType, request.OpeningDeposit);
            return MapAccount(account);
        }

        public CustomerProfileDto OnboardCustomer(OnboardCustomerRequest request)
        {
            Validate(request);

            var customer = repository.AddCustomer(new Customer
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                NationalId = request.NationalId,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                DateOfBirth = request.DateOfBirth,
                Status = CustomerStatus.Active
            });

            repository.OpenAccount(customer.CustomerNumber, request.InitialAccountType, request.OpeningDeposit);
            return GetCustomer(customer.CustomerNumber);
        }

        public BankingOperationResult TransferFunds(TransferFundsRequest request)
        {
            Validate(request);
            var result = repository.TransferFunds(request.FromAccountNumber, request.ToAccountNumber, request.Amount);
            return new BankingOperationResult
            {
                Success = result.Success,
                Message = result.Message,
                ReferenceNumber = result.ReferenceNumber
            };
        }

        public BankingOperationResult MakePayment(PaymentRequest request)
        {
            Validate(request);
            var result = repository.MakePayment(request.AccountNumber, request.Amount, request.MerchantName);
            return new BankingOperationResult
            {
                Success = result.Success,
                Message = result.Message,
                ReferenceNumber = result.ReferenceNumber
            };
        }

        private static void Validate(object request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
        }

        private static CustomerProfileDto MapCustomerProfile(Customer customer)
        {
            return new CustomerProfileDto
            {
                CustomerId = customer.CustomerId,
                CustomerNumber = customer.CustomerNumber,
                FullName = $"{customer.FirstName} {customer.LastName}",
                Email = customer.Email,
                PhoneNumber = customer.PhoneNumber,
                Status = customer.Status.ToString(),
                DateOfBirth = customer.DateOfBirth,
                CreatedOn = customer.CreatedOn,
                Accounts = customer.Accounts.OrderBy(account => account.AccountNumber).Select(account => MapAccount(account, customer)).ToList()
            };
        }

        private static AccountDto MapAccount(Account account, Customer customer = null)
        {
            return new AccountDto
            {
                AccountId = account.AccountId,
                AccountNumber = account.AccountNumber,
                CustomerNumber = customer?.CustomerNumber ?? account.Customer?.CustomerNumber,
                CustomerName = customer == null ? account.Customer == null ? null : $"{account.Customer.FirstName} {account.Customer.LastName}" : $"{customer.FirstName} {customer.LastName}",
                AccountType = account.AccountType.ToString(),
                Status = account.Status.ToString(),
                Balance = account.Balance,
                OpenedOn = account.OpenedOn
            };
        }

        private static TransactionDto MapTransaction(Transaction transaction)
        {
            return new TransactionDto
            {
                ReferenceNumber = transaction.ReferenceNumber,
                AccountNumber = transaction.Account?.AccountNumber,
                Description = transaction.Description,
                TransactionType = transaction.TransactionType.ToString(),
                Amount = transaction.Amount,
                BalanceAfter = transaction.BalanceAfter,
                PostedOn = transaction.PostedOn
            };
        }
    }
}
