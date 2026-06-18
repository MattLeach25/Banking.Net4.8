using System;
using System.Linq;
using System.Web.Mvc;
using LegacyBanking.Application;
using LegacyBanking.Domain;

namespace LegacyBanking.Web.Controllers
{
    public sealed class HomeController : Controller
    {
        private readonly IBankingApplicationService service = new BankingApplicationService();

        public ActionResult Index()
        {
            return View(service.GetDashboard());
        }
    }

    public sealed class CustomersController : Controller
    {
        private readonly IBankingApplicationService service = new BankingApplicationService();

        public ActionResult Index()
        {
            return View(service.GetCustomers());
        }

        public ActionResult Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return HttpNotFound();
            }

            return View(service.GetCustomer(id));
        }

        public ActionResult Create()
        {
            return View(new OnboardCustomerRequest
            {
                DateOfBirth = DateTime.UtcNow.AddYears(-30),
                InitialAccountType = AccountType.Checking,
                OpeningDeposit = 500m
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(OnboardCustomerRequest request)
        {
            if (!ModelState.IsValid)
            {
                return View(request);
            }

            var customer = service.OnboardCustomer(request);
            TempData["StatusMessage"] = $"Customer {customer.CustomerNumber} opened successfully.";
            return RedirectToAction("Details", new { id = customer.CustomerNumber });
        }
    }

    public sealed class AccountsController : Controller
    {
        private readonly IBankingApplicationService service = new BankingApplicationService();

        public ActionResult Index()
        {
            var accounts = service.GetCustomers().SelectMany(customer => customer.Accounts).OrderByDescending(account => account.Balance).ToList();
            return View(accounts);
        }

        public ActionResult Open()
        {
            return View(new OpenAccountRequest
            {
                CustomerNumber = "CUST-100001",
                AccountType = AccountType.Checking,
                OpeningDeposit = 250m
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Open(OpenAccountRequest request)
        {
            if (!ModelState.IsValid)
            {
                return View(request);
            }

            var account = service.OpenAccount(request);
            TempData["StatusMessage"] = $"Account {account.AccountNumber} opened successfully.";
            return RedirectToAction("Index");
        }

        public ActionResult Transfer()
        {
            return View(new TransferFundsRequest { Amount = 100m });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Transfer(TransferFundsRequest request)
        {
            if (!ModelState.IsValid)
            {
                return View(request);
            }

            var result = service.TransferFunds(request);
            TempData["StatusMessage"] = result.Message + (string.IsNullOrWhiteSpace(result.ReferenceNumber) ? string.Empty : " Reference: " + result.ReferenceNumber);
            return RedirectToAction("Index");
        }
    }

    public sealed class PaymentsController : Controller
    {
        private readonly IBankingApplicationService service = new BankingApplicationService();

        public ActionResult Create()
        {
            return View(new PaymentRequest { MerchantName = "Utilities", Amount = 150m });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(PaymentRequest request)
        {
            if (!ModelState.IsValid)
            {
                return View(request);
            }

            var result = service.MakePayment(request);
            TempData["StatusMessage"] = result.Message + (string.IsNullOrWhiteSpace(result.ReferenceNumber) ? string.Empty : " Reference: " + result.ReferenceNumber);
            return RedirectToAction("Index", "Home");
        }
    }
}
