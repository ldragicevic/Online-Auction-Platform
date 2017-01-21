using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using IEP_Projekat.Models;
using System.IO;
using Microsoft.AspNet.Identity;
using PagedList;

namespace IEP_Projekat.Controllers
{
    public class AuctionController : Controller
    {
        private IEP_Model db = new IEP_Model();
        readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        // GET: Auction
        [Authorize(Roles = "Administrator")]
        public ActionResult Index(int? page, string SortOrder = "product", string SortType = "ascending", string change = "false", string SearchString = "")
        {

            logger.Info("Auction // Index // ");

            if (ViewBag.SearchString != null)
                SearchString = ViewBag.SearchString;

            // Change SortType Asc <---> Desc
            if (change.Equals("true")) {
                if (SortType.Equals("ascending")) {
                    SortType = "descending";
                }
                else { 
                    SortType = "ascending";
                }
            }

            ViewBag.SortType = SortType;
            ViewBag.SearchString = SearchString;

            var auctions =  from o
                            in db.auctions
                            select o;

            // Search Pattern is not null
            if (!String.IsNullOrEmpty(SearchString))
            {
                auctions = auctions.Where(
                    s => s.product_name.Contains(SearchString) // Other search criteria
                );
            }

            if (SortType.Equals("ascending")) // Ascending sort
            {
                switch (SortOrder)
                {
                    case "product":
                        auctions = auctions.OrderBy(s => s.product_name);
                        ViewBag.SortOrder = "product";
                        break;
                    case "duration":
                        auctions = auctions.OrderBy(s => s.duration);
                        ViewBag.SortOrder = "duration";
                        break;
                    case "price":
                        auctions = auctions.OrderBy(s => s.price);
                        ViewBag.SortOrder = "price";
                        break;
                    case "state":
                        auctions = auctions.OrderBy(s => s.state);
                        ViewBag.SortOrder = "state";
                        break;
                    case "created":
                        auctions = auctions.OrderBy(s => s.create_date_time);
                        ViewBag.SortOrder = "created";
                        break;
                    case "opened":
                        auctions = auctions.OrderBy(s => s.open_date_time);
                        ViewBag.SortOrder = "opened";
                        break;
                    default:
                        auctions = auctions.OrderBy(s => s.close_date_time);
                        ViewBag.SortOrder = "closed";
                        break;
                }
            }
            else // Descending sort
            {
                {
                    switch (SortOrder)
                    {
                        case "product":
                            auctions = auctions.OrderByDescending(s => s.product_name);
                            ViewBag.SortOrder = "product";
                            break;
                        case "duration":
                            auctions = auctions.OrderByDescending(s => s.duration);
                            ViewBag.SortOrder = "duration";
                            break;
                        case "price":
                            auctions = auctions.OrderByDescending(s => s.price);
                            ViewBag.SortOrder = "price";
                            break;
                        case "state":
                            auctions = auctions.OrderByDescending(s => s.state);
                            ViewBag.SortOrder = "state";
                            break;
                        case "created":
                            auctions = auctions.OrderByDescending(s => s.create_date_time);
                            ViewBag.SortOrder = "created";
                            break;
                        case "opened":
                            auctions = auctions.OrderByDescending(s => s.open_date_time);
                            ViewBag.SortOrder = "opened";
                            break;
                        default:
                            auctions = auctions.OrderByDescending(s => s.close_date_time);
                            ViewBag.SortOrder = "closed";
                            break;
                    }
                }
            }

            int pageSize = 5;
            int pageNumber = (page ?? 1);
            return View(auctions.ToPagedList(pageNumber, pageSize));
        }

        // GET: Auction/Details/5
        public ActionResult Details(long? id, string heading = "")
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            auction auction = db.auctions.Find(id);

            if (auction == null)
                return HttpNotFound();

            if (User.IsInRole("Administrator"))
                ViewBag.UserRole = "Administrator";
            else if (User.IsInRole("Registrovan")) {
                ViewBag.heading = heading;
                ViewBag.UserRole = "Registrovan";
            }
            else
                ViewBag.UserRole = "Gost";

            var productQuery = from b in db.bids
                               where b.IDAuc == id
                               orderby b.tokens descending
                               select b;
            var limitedProductQuery = productQuery.Take(10);

            int count = limitedProductQuery.Count();
            long[] prices = new long[count];
            string[] bidders = new string[count];
            string[] times = new string[count];
            string[] states = new string[count];
            int i = 0;
            foreach (var bid in limitedProductQuery)
            {
                prices[i] = bid.tokens;
                bidders[i] = bid.AspNetUser.Email;
                times[i] = bid.bidTime.ToString(@"dd\:hh\:mm\:ss");
                states[i++] = "Open";
            }
            if (auction.state == "Sold")
            {
                states[0] = "Sold";
            }
            ViewBag.count = count;
            ViewBag.prices = prices;
            ViewBag.bidders = bidders;
            ViewBag.times = times;
            ViewBag.states = states;
            ViewBag.IDAuction = id;
            ViewBag.heading = heading;
            return View(auction);
        }

        // GET: Auction/Create
        [Authorize(Roles = "Administrator")]
        public ActionResult Create()
        {
            return View();
        }

        // POST: Auction/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize(Roles = "Administrator")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "product_name,duration,price,PictureToUpload")] auction auction)
        {
            if (ModelState.IsValid && auction.PictureToUpload != null)
            {
                auction.picture = new byte[auction.PictureToUpload.ContentLength];
                auction.PictureToUpload.InputStream.Read(auction.picture, 0, auction.picture.Length);
                auction.create_date_time = DateTime.Now;
                auction.state = "Ready";
                auction.lastbidder = " ";
                db.auctions.Add(auction);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(auction);
        }

        // GET: Auction/Edit/5
        [Authorize(Roles = "Administrator")]
        public ActionResult Edit(long? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            auction auction = db.auctions.Find(id);
            if (auction.state != "Ready")
            {
                return RedirectToAction("Index");
            }
            if (auction == null)
            {
                return HttpNotFound();
            }
            return View(auction);
        }

        // POST: Auction/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize(Roles = "Administrator")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "IDAuc,price")] EditAuctionViewModel auction)
        {
            if (ModelState.IsValid)
            {
                auction edited = db.auctions.Find(auction.IDAuc);
                // Manually wants to edit Auction, can't if it is not Ready
                if (edited.state != "Ready")
                {
                    return RedirectToAction("Index");
                }
                edited.price = auction.price;
                db.Entry(edited).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(auction);
        }

        [Authorize(Roles = "Administrator")]
        public ActionResult Open(long? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            auction auction = db.auctions.Find(id);
            if (auction == null || auction.state == "Open")
            {
                return HttpNotFound();
            }
            auction.state = "Open";
            auction.open_date_time = DateTime.Now;
            auction.close_date_time = DateTime.Now.AddSeconds(auction.duration);
            db.Entry(auction).State = EntityState.Modified;
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        // GET: Auction/Delete/5
        [Authorize(Roles = "Administrator")]
        public ActionResult Delete(long? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            auction auction = db.auctions.Find(id);
            if (auction == null || auction.state != "Ready")
            {
                return HttpNotFound();
            }
            return View(auction);
        }

        // POST: Auction/Delete/5
        [Authorize(Roles = "Administrator")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(long id)
        {
            auction auction = db.auctions.Find(id);
            auction.state = "Deleted";
            auction.open_date_time = DateTime.Now;
            auction.close_date_time = DateTime.Now;
            auction.duration = 0;
            db.Entry(auction).State = EntityState.Modified;
            db.SaveChanges();
            return RedirectToAction("Index");
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
