using System.Web.Mvc;
using System.Web.Routing;
using LegacyBanking.Data;

namespace LegacyBanking.Web
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            BankingDatabase.Initialize();
            AreaRegistration.RegisterAllAreas();
            RouteConfig.RegisterRoutes(RouteTable.Routes);
        }
    }
}
