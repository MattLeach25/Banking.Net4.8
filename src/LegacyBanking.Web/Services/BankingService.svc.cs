using System.ServiceModel;
using LegacyBanking.Application;

namespace LegacyBanking.Web.Services
{
    [ServiceBehavior(IncludeExceptionDetailInFaults = true)]
    public sealed class BankingService : IBankingService
    {
        private readonly IBankingApplicationService service = new BankingApplicationService();

        public BankingDashboardDto GetDashboard()
        {
            return service.GetDashboard();
        }

        public CustomerProfileDto GetCustomer(string customerNumber)
        {
            return service.GetCustomer(customerNumber);
        }

        public CustomerProfileDto OnboardCustomer(OnboardCustomerRequest request)
        {
            return service.OnboardCustomer(request);
        }

        public AccountDto OpenAccount(OpenAccountRequest request)
        {
            return service.OpenAccount(request);
        }

        public BankingOperationResult TransferFunds(TransferFundsRequest request)
        {
            return service.TransferFunds(request);
        }

        public BankingOperationResult MakePayment(PaymentRequest request)
        {
            return service.MakePayment(request);
        }
    }
}
