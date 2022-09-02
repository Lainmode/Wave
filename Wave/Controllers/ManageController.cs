using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Wave.Models;

namespace Wave.Controllers
{
    public class ManageController : Controller
    {
        // GET: Manage
        WaveEntities db = new WaveEntities();
        HttpCookie cookie;
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            Common.Log(filterContext, db); // Log the request

            if (!Request.Cookies.AllKeys.Contains(Common.CookieName))
            {
                filterContext.Result = new HttpStatusCodeResult(HttpStatusCode.Forbidden, "Forbidden");
                return;
            }
            cookie = Request.Cookies.Get(Common.CookieName);
            if (cookie.Value != Common.CookieSecret)
            {
                filterContext.Result = new HttpStatusCodeResult(HttpStatusCode.Forbidden, "Forbidden");
                return;
            }
            base.OnActionExecuting(filterContext);
        }
        [HttpPost]
        public JsonResult BanUser(string loyaltyCardGUID)
        {
            Customer customer = db.Customers.Where(e => e.LoyaltyCardGUID == loyaltyCardGUID).FirstOrDefault();
            customer.IsActive = false;
            customer.IsBanned = true;
            db.Entry(customer).State = System.Data.Entity.EntityState.Modified;
            db.SaveChanges();
            return Json(Common.BuildGeneralResponseJson(true, ResponseCode.Banned, "Successfully banned user."));
        }

        [HttpPost]
        public JsonResult UnbanUser(string loyaltyCardGUID)
        {
            Customer customer = db.Customers.Where(e => e.LoyaltyCardGUID == loyaltyCardGUID).FirstOrDefault();
            customer.IsActive = true;
            customer.IsBanned = false;
            db.Entry(customer).State = System.Data.Entity.EntityState.Modified;
            db.SaveChanges();
            return Json(Common.BuildGeneralResponseJson(true, ResponseCode.RequestFulfilled, "Successfully unbanned user."));
        }

        [HttpPost]
        public JsonResult ModifyLoyaltyPoints(string loyaltyCardGUID, int newValue)
        {
            if (newValue <= 6)
            {
                Customer customer = db.Customers.Where(e => e.LoyaltyCardGUID == loyaltyCardGUID).FirstOrDefault();
                customer.LoyaltyPoints = newValue;
                customer.Scans.Add(new Scan() { ScanDate = DateTime.Now });
                db.Entry(customer).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();
                return Json(Common.BuildGeneralResponseJson(true, ResponseCode.LoyaltyPointsModified, "Successfully modified loyalty points."));
            }
            else
            {
                return Json(Common.BuildGeneralResponseJson(false, ResponseCode.GeneralError, "New value is greater than the maximum amount allowed."));
            }
        }
    }
}