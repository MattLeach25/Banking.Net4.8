using Xunit;

// Disable parallel test execution because BankingDataStore is a static class
// with shared state. Tests must run sequentially to avoid race conditions.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
