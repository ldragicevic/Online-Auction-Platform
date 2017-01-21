using IEP_Projekat.Models;
using log4net;
using PagedList;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace IEP_Projekat.Controllers
{
    public class HomeController : Controller
    {

        private IEP_Model db = new IEP_Model();
        readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public ActionResult Index(int? page, string searchState, string searchMin = "", string searchMax = "", string searchName = "", string heading = "live")
        {
            logger.Info("Home // Index // ");
            if (User.IsInRole("Registrovan"))
                return RedirectToAction("Index", "Registred"); // Registrovan
            else if (User.IsInRole("Administrator"))
                return RedirectToAction("Index", "Administrator"); // Administrator

            // Guest Index Page
            ViewBag.heading = heading;
            ViewBag.searchMin = searchMin;
            ViewBag.searchMax = searchMax;
            ViewBag.searchName = searchName;

            var auctions = from o in db.auctions select o;
            if (heading.Equals("live"))
                auctions = auctions.Where(a => a.state == "Open");
            else if (heading.Equals("upcoming"))
                auctions = auctions.Where(a => a.state == "Ready");
            else // if (heading.Equals("past")) 
                auctions = auctions.Where(a => a.state == "Sold" || a.state == "Expired");

            // FILTER by NAME
            if (!String.IsNullOrEmpty(searchName))
                auctions = auctions.Where(a => a.product_name.Contains(searchName));

            // FILTER by MIN MAX
            if (!String.IsNullOrEmpty(searchMin) || !String.IsNullOrEmpty(searchMax))
            {
                long auctionMinPrice = 0;
                long auctionMaxPrice = long.MaxValue;
                if (!String.IsNullOrEmpty(searchMin))
                {
                    auctionMinPrice = Convert.ToInt64(searchMin);
                    ViewBag.searchMin = auctionMinPrice;
                }
                if (!String.IsNullOrEmpty(searchMax))
                {
                    auctionMaxPrice = Convert.ToInt64(searchMax);
                    ViewBag.searchMax = auctionMaxPrice;
                }
                auctions = auctions.Where(a => (a.price + a.increment) >= auctionMinPrice && (a.price + a.increment) <= auctionMaxPrice);
            }

            // Order: Earliest first
            auctions = auctions.OrderBy(s => s.close_date_time);

            int pageSize = 8;
            int pageNumber = (page ?? 1);
            return View(auctions.ToPagedList(pageNumber, pageSize));
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";
            return View();
        }
    }
}