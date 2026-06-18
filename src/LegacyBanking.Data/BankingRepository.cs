using System;
using System.Collections.Generic;
using System.Linq;
using LegacyBanking.Domain;

namespace LegacyBanking.Data
{
    public sealed class BankingRepository
    {
        public IReadOnlyList<Customer> GetCustomers()
        {
            var graph = BuildGraph(BankingDataStore.Load());
            return graph.Customers
                .OrderBy(customer => customer.LastName)
                .ThenBy(customer => customer.FirstName)
                .ToList();
        }

        public Customer GetCustomerByNumber(string customerNumber)
        {
            var graph = BuildGraph(BankingDataStore.Load());
            return graph.Customers.SingleOrDefault(customer => customer.CustomerNumber == customerNumber);
        }

        public Account GetAccountByNumber(string accountNumber)
        {
            var graph = BuildGraph(BankingDataStore.Load());
            return graph.Accounts.SingleOrDefault(account => account.AccountNumber == accountNumber);
        }

        public Customer AddCustomer(Customer customer)
        {
            return BankingDataStore.ExecuteWrite(data =>
            {
                var customerId = data.NextCustomerId++;
                customer.CreatedOn = DateTime.UtcNow;
                customer.Status = CustomerStatus.Active;
                customer.CustomerId = customerId;
                customer.CustomerNumber = GenerateCustomerNumber(data);

                data.Customers.Add(new CustomerRecord
                {
                    CustomerId = customer.CustomerId,
                    CustomerNumber = customer.CustomerNumber,
                    FirstName = customer.FirstName,
                    LastName = customer.LastName,
                    NationalId = customer.NationalId,
                    Email = customer.Email,
                    PhoneNumber = customer.PhoneNumber,
                    DateOfBirth = customer.DateOfBirth,
                    Status = customer.Status,
                    CreatedOn = customer.CreatedOn
                });

                return customer;
            });
        }

        public Account OpenAccount(string customerNumber, AccountType accountType, decimal openingDeposit)
        {
            return BankingDataStore.ExecuteWrite(data =>
            {
                var customer = data.Customers.SingleOrDefault(item => item.CustomerNumber == customerNumber);
                if (customer == null)
                {
                    throw new InvalidOperationException("Customer not found.");
                }

                var account = new AccountRecord
                {
                    AccountId = data.NextAccountId++,
                    CustomerId = customer.CustomerId,
                    AccountNumber = GenerateAccountNumber(data),
                    AccountType = accountType,
                    Status = AccountStatus.Active,
                    Balance = 0m,
                    OpenedOn = DateTime.UtcNow
                };

                data.Accounts.Add(account);

                if (openingDeposit > 0m)
                {
                    PostTransaction(data, account.AccountId, TransactionType.OpeningDeposit, openingDeposit, "Opening deposit", openingDeposit, null, null);
                }

                var graph = BuildGraph(data);
                return graph.Accounts.Single(item => item.AccountId == account.AccountId);
            });
        }

        public Transaction Deposit(string accountNumber, decimal amount, string description)
        {
            return BankingDataStore.ExecuteWrite(data =>
            {
                var account = data.Accounts.SingleOrDefault(item => item.AccountNumber == accountNumber);
                if (account == null)
                {
                    throw new InvalidOperationException("Account not found.");
                }

                return PostTransaction(data, account.AccountId, TransactionType.Deposit, amount, description, account.Balance + amount, null, null);
            });
        }

        public Transaction Withdraw(string accountNumber, decimal amount, string description)
        {
            return BankingDataStore.ExecuteWrite(data =>
            {
                var account = data.Accounts.SingleOrDefault(item => item.AccountNumber == accountNumber);
                if (account == null)
                {
                    throw new InvalidOperationException("Account not found.");
                }

                if (account.Balance < amount)
                {
                    throw new InvalidOperationException("Insufficient funds.");
                }

                return PostTransaction(data, account.AccountId, TransactionType.Withdrawal, amount, description, account.Balance - amount, null, null);
            });
        }

        public (bool Success, string Message, string ReferenceNumber) TransferFunds(string fromAccountNumber, string toAccountNumber, decimal amount)
        {
            return BankingDataStore.ExecuteWrite(data =>
            {
                var fromAccount = data.Accounts.SingleOrDefault(item => item.AccountNumber == fromAccountNumber);
                var toAccount = data.Accounts.SingleOrDefault(item => item.AccountNumber == toAccountNumber);

                if (fromAccount == null || toAccount == null)
                {
                    return (false, "One or both accounts were not found.", null);
                }

                if (fromAccount.AccountId == toAccount.AccountId)
                {
                    return (false, "Source and destination accounts must be different.", null);
                }

                if (fromAccount.Balance < amount)
                {
                    return (false, "Insufficient funds.", null);
                }

                var reference = GenerateReferenceNumber(data);
                PostTransaction(data, fromAccount.AccountId, TransactionType.TransferOut, amount, "Internal transfer out", fromAccount.Balance - amount, toAccount.AccountId, reference);
                PostTransaction(data, toAccount.AccountId, TransactionType.TransferIn, amount, "Internal transfer in", toAccount.Balance + amount, fromAccount.AccountId, reference);

                return (true, "Transfer completed.", reference);
            });
        }

        public (bool Success, string Message, string ReferenceNumber) MakePayment(string accountNumber, decimal amount, string merchantName)
        {
            return BankingDataStore.ExecuteWrite(data =>
            {
                var account = data.Accounts.SingleOrDefault(item => item.AccountNumber == accountNumber);
                if (account == null)
                {
                    return (false, "Account not found.", null);
                }

                if (account.Balance < amount)
                {
                    return (false, "Insufficient funds.", null);
                }

                var reference = GenerateReferenceNumber(data);
                PostTransaction(data, account.AccountId, TransactionType.Payment, amount, $"Card or bill payment to {merchantName}", account.Balance - amount, null, reference);
                return (true, "Payment submitted.", reference);
            });
        }

        public IReadOnlyList<Transaction> GetRecentTransactions(int take = 20)
        {
            var graph = BuildGraph(BankingDataStore.Load());
            return graph.Transactions
                .OrderByDescending(transaction => transaction.PostedOn)
                .Take(take)
                .ToList();
        }

        private static Transaction PostTransaction(BankingDataFile data, int accountId, TransactionType transactionType, decimal amount, string description, decimal balanceAfter, int? counterpartyAccountId, string referenceNumber)
        {
            var account = data.Accounts.Single(item => item.AccountId == accountId);
            account.Balance = balanceAfter;

            var transaction = new TransactionRecord
            {
                TransactionId = data.NextTransactionId++,
                AccountId = accountId,
                PostedOn = DateTime.UtcNow,
                TransactionType = transactionType,
                Amount = amount,
                BalanceAfter = balanceAfter,
                Description = description,
                ReferenceNumber = referenceNumber ?? GenerateReferenceNumber(data),
                CounterpartyAccountId = counterpartyAccountId
            };

            data.Transactions.Add(transaction);

            var graph = BuildGraph(data);
            return graph.Transactions.Single(item => item.TransactionId == transaction.TransactionId);
        }

        private static string GenerateCustomerNumber(BankingDataFile data)
        {
            var lastNumber = data.Customers
                .Select(customer => customer.CustomerNumber)
                .Select(value => ExtractSequence(value, "CUST-"))
                .DefaultIfEmpty(100000)
                .Max();

            return $"CUST-{lastNumber + 1:D6}";
        }

        private static string GenerateAccountNumber(BankingDataFile data)
        {
            var lastNumber = data.Accounts
                .Select(account => account.AccountNumber)
                .Select(value => ExtractSequence(value, "ACCT-"))
                .DefaultIfEmpty(700000)
                .Max();

            return $"ACCT-{lastNumber + 1:D6}";
        }

        private static string GenerateReferenceNumber(BankingDataFile data)
        {
            var lastReference = data.Transactions
                .Select(transaction => transaction.ReferenceNumber)
                .Select(value => ExtractSequence(value, "REF-"))
                .DefaultIfEmpty(0)
                .Max();

            return $"REF-{lastReference + 1:D6}";
        }

        private static int ExtractSequence(string value, string prefix)
        {
            if (string.IsNullOrWhiteSpace(value) || !value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return 0;
            }

            var sequence = value.Substring(prefix.Length);
            int parsed;
            return int.TryParse(sequence, out parsed) ? parsed : 0;
        }

        private static RepositoryGraph BuildGraph(BankingDataFile data)
        {
            var customers = data.Customers
                .Select(customer => new Customer
                {
                    CustomerId = customer.CustomerId,
                    CustomerNumber = customer.CustomerNumber,
                    FirstName = customer.FirstName,
                    LastName = customer.LastName,
                    NationalId = customer.NationalId,
                    Email = customer.Email,
                    PhoneNumber = customer.PhoneNumber,
                    DateOfBirth = customer.DateOfBirth,
                    Status = customer.Status,
                    CreatedOn = customer.CreatedOn
                })
                .ToDictionary(customer => customer.CustomerId);

            var accounts = data.Accounts
                .Select(account => new Account
                {
                    AccountId = account.AccountId,
                    AccountNumber = account.AccountNumber,
                    CustomerId = account.CustomerId,
                    AccountType = account.AccountType,
                    Status = account.Status,
                    Balance = account.Balance,
                    OpenedOn = account.OpenedOn
                })
                .ToDictionary(account => account.AccountId);

            foreach (var account in accounts.Values)
            {
                Customer customer;
                if (customers.TryGetValue(account.CustomerId, out customer))
                {
                    account.Customer = customer;
                    customer.Accounts.Add(account);
                }
            }

            var transactions = data.Transactions
                .Select(transaction => new Transaction
                {
                    TransactionId = transaction.TransactionId,
                    AccountId = transaction.AccountId,
                    PostedOn = transaction.PostedOn,
                    TransactionType = transaction.TransactionType,
                    Amount = transaction.Amount,
                    BalanceAfter = transaction.BalanceAfter,
                    Description = transaction.Description,
                    ReferenceNumber = transaction.ReferenceNumber,
                    CounterpartyAccountId = transaction.CounterpartyAccountId
                })
                .ToList();

            foreach (var transaction in transactions)
            {
                Account account;
                if (accounts.TryGetValue(transaction.AccountId, out account))
                {
                    transaction.Account = account;
                    account.Transactions.Add(transaction);
                }
            }

            return new RepositoryGraph
            {
                Customers = customers.Values.ToList(),
                Accounts = accounts.Values.ToList(),
                Transactions = transactions
            };
        }

        private sealed class RepositoryGraph
        {
            public IReadOnlyList<Customer> Customers { get; set; }
            public IReadOnlyList<Account> Accounts { get; set; }
            public IReadOnlyList<Transaction> Transactions { get; set; }
        }
    }
}
