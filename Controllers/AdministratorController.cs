using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace IEP_Projekat.Controllers
{
    public class AdministratorController : Controller
    {
        // Log4net
        readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        // GET: Administrator
        [Authorize(Roles = "Administrator")]
        public ActionResult Index()
        {
            logger.Info("Administrator // Index // ");
            return RedirectToAction("Index", "Auction", new { area = "" });
        }
    }
}