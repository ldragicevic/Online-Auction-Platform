using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using IEP_Projekat.Models;
using Microsoft.AspNet.Identity;
using System.Threading.Tasks;
using System.Text;
using PagedList;

namespace IEP_Projekat.Controllers
{
    public class OrderController : Controller
    {
        private IEP_Model db = new IEP_Model();
        readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        // GET: Order
        [Authorize(Roles = "Registrovan")]
        public ActionResult Index(int? page, string SearchString,  string SortOrder = "Id", string SortType = "ascending", string change = "false")
        {

            logger.Info("Order // Index // ");

            // Ascending <---> Descending
            if (change.Equals("true"))
            {
                if (SortType.Equals("ascending"))
                {
                    SortType = "descending";
                }
                else
                {
                    SortType = "ascending";
                }
            }

            ViewBag.SortType = SortType;

            //SortOrder = String.IsNullOrEmpty(SortOrder) ? "Id" : SortOrder;
            //ViewBag.SortOrder = SortOrder;

            var UserId = User.Identity.GetUserId();
            var orders = from o in db.orders
                         where o.Id == UserId
                         select o;

            // Search Pattern is not null
            if (!String.IsNullOrEmpty(SearchString))
            {
                orders = orders.Where(
                    s => s.status.Contains(SearchString) // Other search criteria
                );
            }

            if (SortType.Equals("ascending")) // Ascending sort
            {
                switch (SortOrder)
                {
                    case "Id":
                        orders = orders.OrderBy(s => s.IDOrd);
                        ViewBag.SortOrder = "Id";
                        break;
                    case "email":
                        orders = orders.OrderBy(s => s.AspNetUser.Email);
                        ViewBag.SortOrder = "email";
                        break;
                    case "tokenNumber":
                        orders = orders.OrderBy(s => s.token_number);
                        ViewBag.SortOrder = "tokenNumber";
                        break;
                    case "price":
                        orders = orders.OrderBy(s => s.package_price);
                        ViewBag.SortOrder = "price";
                        break;
                    case "status":
                        orders = orders.OrderBy(s => s.status);
                        ViewBag.SortOrder = "status";
                        break;
                    default:
                        orders = orders.OrderBy(s => s.create_date_time);
                        ViewBag.SortOrder = "create";
                        break;
                }
            }
            else // Descending sort
            {
                switch (SortOrder)
                {
                    case "Id":
                        orders = orders.OrderByDescending(s => s.IDOrd);
                        ViewBag.SortOrder = "Id";
                        break;
                    case "email":
                        orders = orders.OrderByDescending(s => s.AspNetUser.Email);
                        ViewBag.SortOrder = "email";
                        break;
                    case "tokenNumber":
                        orders = orders.OrderByDescending(s => s.token_number);
                        ViewBag.SortOrder = "tokenNumber";
                        break;
                    case "price":
                        orders = orders.OrderByDescending(s => s.package_price);
                        ViewBag.SortOrder = "price";
                        break;
                    case "status":
                        orders = orders.OrderByDescending(s => s.status);
                        ViewBag.SortOrder = "status";
                        break;
                    default:
                        orders = orders.OrderByDescending(s => s.create_date_time);
                        ViewBag.SortOrder = "create";
                        break;
                }
            }

            int pageSize = 5;
            int pageNumber = (page ?? 1);
            return View(orders.ToPagedList(pageNumber, pageSize));
        }

        // GET: Order/Details/5
        [Authorize(Roles = "Registrovan")]
        public ActionResult Details(long? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            order order = db.orders.Find(id);
            var UserId = User.Identity.GetUserId();
            // Cannot display other's Order
            if (order.Id != UserId || order == null)
            {
                return HttpNotFound();
            }
            return View(order);
        }

        // GET: Order/Create
        [Authorize(Roles = "Registrovan")]
        public ActionResult Create()
        {
            order NewOrder = new order();
            NewOrder.create_date_time = DateTime.Now;
            var CurrentUser = db.AspNetUsers.Find(User.Identity.GetUserId());
            NewOrder.AspNetUser = CurrentUser;
            NewOrder.status = "waiting";

            db.orders.Add(NewOrder);
            db.SaveChanges();

            String crypted = Convert.ToString(NewOrder.IDOrd);
            crypted = crypted + "1284181719";

            var CentiliLink = "http://stage.centili.com/widget/WidgetModule?api=9da39076a3f651a6885d1c5135792b20" + "&clientID=" + crypted;
            return Redirect(CentiliLink);
        }

        public HttpStatusCodeResult Update(string clientId, string status, long amount, double enduserprice)
        {
            // Invalid request, crypted key not valid
            if (clientId.Length < 10 || clientId.Substring(clientId.Length - 10, 10) != "1284181719") {
                return new HttpStatusCodeResult(200);
            }

            long IDOrd = Convert.ToInt64(clientId.Substring(0, clientId.Length - 10));

            // SUCCESS
            if (status.Equals("success"))
            {
                order Order = db.orders.Find(IDOrd);
                AspNetUser CurrentUser = Order.AspNetUser;
                CurrentUser.TokenNumber += amount;
                Order.status = "successful";
                db.Entry(Order).State = EntityState.Modified;
                db.SaveChanges();

                System.Net.Mail.MailMessage message = new System.Net.Mail.MailMessage();
                String emailAddress = "lukadragic032@gmail.com";
                //String emailAddress = User.Email;
                
                message.From = new System.Net.Mail.MailAddress("iepaukcija@mail.com");
                message.To.Add(new System.Net.Mail.MailAddress(emailAddress));
                message.IsBodyHtml = true;
                message.BodyEncoding = Encoding.UTF8;
                message.Subject = "Auction: Token order";
                message.Body = DateTime.Now + " you have bought " + amount + " tokens " + " - price " + enduserprice + " dinars.";
                System.Net.Mail.SmtpClient client = new System.Net.Mail.SmtpClient();
                client.Send(message);
            }
            else // FAILED
            {
                order Order = db.orders.Find(IDOrd);
                Order.status = "failed";
                db.Entry(Order).State = EntityState.Modified;
                db.SaveChanges();
            }

            return new HttpStatusCodeResult(200);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
