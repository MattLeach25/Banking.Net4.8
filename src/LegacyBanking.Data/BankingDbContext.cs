using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using LegacyBanking.Domain;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.Configuration;


namespace LegacyBanking.Data
{
    public static class BankingDatabase
    {
        public static void Initialize()
        {
            BankingDataStore.Initialize();
        }
    }

    internal static class BankingDataStore
    {
        private static readonly object SyncRoot = new object();
        private static readonly DataContractJsonSerializer Serializer = new DataContractJsonSerializer(typeof(BankingDataFile));
        private static bool initialized;

        internal static void Initialize()
        {
            lock (SyncRoot)
            {
                if (initialized)
                {
                    return;
                }

                string connectionString = ConfigurationManager.AppSettings["StorageConnectionString"];
                string containerName = "test";
                string blobName = "LegacyBankingDb.txt";

                var blobClient = new BlobClient(connectionString, containerName, blobName);

                if (!blobClient.Exists())
                {
                    Save(LoadSeedData());
                }

                initialized = true;
            }
        }

        internal static BankingDataFile Load()
        {
            lock (SyncRoot)
            {
                string connectionString = ConfigurationManager.AppSettings["StorageConnectionString"];
                string containerName = "test";
                string blobName = "LegacyBankingDb.txt";

                var blobClient = new BlobClient(connectionString, containerName, blobName);

                Initialize();
                {
                    var downloadedFile = blobClient.DownloadContent();
                    var stream = downloadedFile.Value.Content.ToStream();
                    var data = Serializer.ReadObject(stream) as BankingDataFile;
                    if (data == null)
                    {
                        throw new InvalidOperationException("Could not read banking data store.");
                    }

                    data.Customers = data.Customers ?? new List<CustomerRecord>();
                    data.Accounts = data.Accounts ?? new List<AccountRecord>();
                    data.Transactions = data.Transactions ?? new List<TransactionRecord>();
                    return data;
                }
            }
        }

        internal static T ExecuteWrite<T>(Func<BankingDataFile, T> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            lock (SyncRoot)
            {
                Initialize();
                var data = Load();
                var result = action(data);
                Save(data);
                return result;
            }
        }

        internal static void ExecuteWrite(Action<BankingDataFile> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            ExecuteWrite(data =>
            {
                action(data);
                return 0;
            });
        }

        private static void Save(BankingDataFile data)
        {
            string connectionString = ConfigurationManager.AppSettings["StorageConnectionString"];
            string containerName = "test";
            string blobName = "LegacyBankingDb.txt";

            var blobClient = new BlobClient(connectionString, containerName, blobName);
            using (var stream = new MemoryStream())
            {
                Serializer.WriteObject(stream, data);
                stream.Position = 0;
                blobClient.Upload(stream, overwrite: true);
            }
        }


        private static BankingDataFile LoadSeedData()
        {
            var seedPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "seed-data.json");
            if (File.Exists(seedPath))
            {
                using (var stream = File.OpenRead(seedPath))
                {
                    var data = Serializer.ReadObject(stream) as BankingDataFile;
                    if (data != null)
                    {
                        return data;
                    }
                }
            }
            
            // Fallback: return empty structure if seed file not found
            return new BankingDataFile
            {
                NextCustomerId = 1,
                NextAccountId = 1,
                NextTransactionId = 1,
                Customers = new List<CustomerRecord>(),
                Accounts = new List<AccountRecord>(),
                Transactions = new List<TransactionRecord>()
            };
        }
    }

    [DataContract]
    internal sealed class BankingDataFile
    {
        [DataMember]
        public int NextCustomerId { get; set; } = 1;

        [DataMember]
        public int NextAccountId { get; set; } = 1;

        [DataMember]
        public int NextTransactionId { get; set; } = 1;

        [DataMember]
        public List<CustomerRecord> Customers { get; set; } = new List<CustomerRecord>();

        [DataMember]
        public List<AccountRecord> Accounts { get; set; } = new List<AccountRecord>();

        [DataMember]
        public List<TransactionRecord> Transactions { get; set; } = new List<TransactionRecord>();
    }

    [DataContract]
    internal sealed class CustomerRecord
    {
        [DataMember]
        public int CustomerId { get; set; }

        [DataMember]
        public string CustomerNumber { get; set; }

        [DataMember]
        public string FirstName { get; set; }

        [DataMember]
        public string LastName { get; set; }

        [DataMember]
        public string NationalId { get; set; }

        [DataMember]
        public string Email { get; set; }

        [DataMember]
        public string PhoneNumber { get; set; }

        [DataMember]
        public DateTime DateOfBirth { get; set; }

        [DataMember]
        public CustomerStatus Status { get; set; }

        [DataMember]
        public DateTime CreatedOn { get; set; }
    }

    [DataContract]
    internal sealed class AccountRecord
    {
        [DataMember]
        public int AccountId { get; set; }

        [DataMember]
        public string AccountNumber { get; set; }

        [DataMember]
        public int CustomerId { get; set; }

        [DataMember]
        public AccountType AccountType { get; set; }

        [DataMember]
        public AccountStatus Status { get; set; }

        [DataMember]
        public decimal Balance { get; set; }

        [DataMember]
        public DateTime OpenedOn { get; set; }
    }

    [DataContract]
    internal sealed class TransactionRecord
    {
        [DataMember]
        public int TransactionId { get; set; }

        [DataMember]
        public int AccountId { get; set; }

        [DataMember]
        public DateTime PostedOn { get; set; }

        [DataMember]
        public TransactionType TransactionType { get; set; }

        [DataMember]
        public decimal Amount { get; set; }

        [DataMember]
        public decimal BalanceAfter { get; set; }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public string ReferenceNumber { get; set; }

        [DataMember]
        public int? CounterpartyAccountId { get; set; }
    }
}
