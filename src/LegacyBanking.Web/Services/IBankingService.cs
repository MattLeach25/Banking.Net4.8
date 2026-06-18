using System.ServiceModel;
using LegacyBanking.Application;

namespace LegacyBanking.Web.Services
{
    [ServiceContract]
    public interface IBankingService
    {
        [OperationContract]
        BankingDashboardDto GetDashboard();

        [OperationContract]
        CustomerProfileDto GetCustomer(string customerNumber);

        [OperationContract]
        CustomerProfileDto OnboardCustomer(OnboardCustomerRequest request);

        [OperationContract]
        AccountDto OpenAccount(OpenAccountRequest request);

        [OperationContract]
        BankingOperationResult TransferFunds(TransferFundsRequest request);

        [OperationContract]
        BankingOperationResult MakePayment(PaymentRequest request);
    }
}
