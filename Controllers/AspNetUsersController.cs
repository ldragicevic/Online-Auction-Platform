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

namespace IEP_Projekat.Controllers
{
    public class AspNetUsersController : Controller
    {
        private IEP_Model db = new IEP_Model();
        readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        
        // GET: AspNetUsers
        [Authorize(Roles = "Administrator" + "," + "Registrovan")]
        public ActionResult Index()
        {
            logger.Info("AspNetUsers // Index // ");
            // Registred user redirects
            if (User.IsInRole("Registrovan"))
                return RedirectToAction("Index", "Registred");
            // Administrator gets list of Users
            return View(db.AspNetUsers.ToList());
        }

        // GET: AspNetUsers/Details/5
        [Authorize(Roles = "Administrator")]
        public ActionResult Details(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            AspNetUser aspNetUser = db.AspNetUsers.Find(id);
            if (aspNetUser == null)
            {
                return HttpNotFound();
            }
            return View(aspNetUser);
        }

        // GET: AspNetUsers/Edit/5
        [Authorize(Roles = "Registrovan")]
        public ActionResult Edit()
        {
            var UserId = User.Identity.GetUserId();
            AspNetUser user = db.AspNetUsers.Find(UserId);
            return View(user);
        }

        // POST: AspNetUsers/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize(Roles = "Registrovan")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Name,Surname")] AspNetUser aspNetUser)
        {
            var UserId = User.Identity.GetUserId();
            AspNetUser user = db.AspNetUsers.Find(UserId);
            user.Name = aspNetUser.Name;
            user.Surname = aspNetUser.Surname;
            db.Entry(user).State = EntityState.Modified;
            db.SaveChanges();
            return RedirectToAction("Index", "Registred");
        }

        // GET: AspNetUsers/Delete/5
        [Authorize(Roles = "Administrator")]
        public ActionResult Delete(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            AspNetUser aspNetUser = db.AspNetUsers.Find(id);
            if (aspNetUser == null)
            {
                return HttpNotFound();
            }
            return View(aspNetUser);
        }

        // POST: AspNetUsers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public ActionResult DeleteConfirmed(string id)
        {
            AspNetUser aspNetUser = db.AspNetUsers.Find(id);
            db.AspNetUsers.Remove(aspNetUser);
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
