using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using Wave.Models;

namespace Wave.Controllers
{
    public class ApiController : Controller
    {
        public SessionData sessionData;
        public WaveEntities db = new WaveEntities();
        public bool IsAdmin = false;
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            Common.Log(filterContext, db); // Log the request

            if (Request.Cookies.AllKeys.Contains("WaveSession"))
                filterContext.Result = RedirectToAction("Index");
            if (Session["SessionData"] == null)
                filterContext.Result = RedirectToAction("Index", "authorization");

            sessionData = (SessionData)Session["SessionData"];
            base.OnActionExecuting(filterContext);
        }
        // GET: Api
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public JsonResult SetOTP(string phoneNumber)
        {
            if ((sessionData.RequestOTPAttemps < 3))
            {
                Random random = new Random();
                string otp = random.Next(1000, 10000).ToString(); // generates otp between 100k and 1mil (no 000001)
                sessionData.OTP = otp;
                sessionData.RequestOTPAttemps++;
                // Send OTP to phone code goes here
                Common.SendOTPMessage(phoneNumber, otp);
                return Json(Common.BuildGeneralResponseJson(true, ResponseCode.OTPRequestSuccessful, "OTP Sent!"));
            }
            return Json(Common.BuildGeneralResponseJson(false, ResponseCode.ExceededMaximumOTPRequests, "Exceeded maximum tries, please try again in 1 hour."));
        }

        [HttpPost]
        [AllowAnonymous]
        //[ValidateAntiForgeryToken]
        public ActionResult SubmitOTP(string otp)
        {
            if (sessionData.SubmitOTPAttempts < 3 && sessionData.SubmitOTPCooldown < DateTime.Now)
            {
                if (otp == sessionData.OTP)
                {
                    // register phone here and create all the stuff uknow 
                    // incomplete it does absolutely nothing rn
                    Customer customer;
                    if (!Common.CheckIfPhoneNumberAlreadyRegistered(sessionData.PhoneNumber)) 
                    {
                        customer = Common.AddNewCustomer(sessionData.PhoneNumber);
                        FormsAuthentication.SetAuthCookie(sessionData.PhoneNumber, true);
                    }
                    else
                    {
                        customer = db.Customers.Where(e => e.PhoneNumber == sessionData.PhoneNumber).FirstOrDefault();
                    }
                    HttpCookie cookie = new HttpCookie("WaveSession");
                    cookie.Expires = DateTime.Now.AddYears(50);
                    cookie.Value = AesOperation.EncryptString(AesOperation.key, customer.Cookie);
                    Response.Cookies.Add(cookie);
                    return Json(Common.BuildGeneralResponseJson(true, ResponseCode.OTPVerificationSuccessful, "Phone number verified!", "http://localhost/User?guid=" + customer.LoyaltyCardGUID));
                }
                sessionData.SubmitOTPAttempts++;
                return Json(Common.BuildGeneralResponseJson(false, ResponseCode.IncorrectOTP, "Incorrect OTP!"));
            }
            else
            {
                if(sessionData.SubmitOTPCooldown != new DateTime())
                {
                    sessionData.SubmitOTPCooldown = DateTime.Now.AddHours(1);
                }
                sessionData.IsSubmitOTPRestricted = true;
                sessionData.SubmitOTPAttempts = 0;
            }

            return Json(Common.BuildGeneralResponseJson(false, ResponseCode.ExceededMaximumOTPSubmissions, "Exceeded maximum tries, please try again in 1 hour."));
        }

        [HttpPost]
        public JsonResult RetrieveCustomerInformation(string loyaltyCardGUID)
        {
            
            Customer customer = db.Customers.Where(e => e.LoyaltyCardGUID == loyaltyCardGUID).FirstOrDefault();
            if(customer != null)
            {
                return Json(Common.BuildDataResponseJson(true, ResponseCode.RequestFulfilled, "Successfully retrieved data.", JsonConvert.SerializeObject(customer)));
            }

            else
            {
                return Json(Common.BuildGeneralResponseJson(false, ResponseCode.GeneralError, "Customer does not exist!"));
            }
        }

        [HttpPost] [Authorize]
        public JsonResult ModifyLoyaltyPoints(string loyaltyCardGUID, int newValue)
        {
            if(newValue <= 6)
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

        public JsonResult BanUser(string loyaltyCardGUID)
        {
            Customer customer = db.Customers.Where(e => e.LoyaltyCardGUID == loyaltyCardGUID).FirstOrDefault();
            customer.IsActive = false;
            customer.IsBanned = true;
            db.Entry(customer).State = System.Data.Entity.EntityState.Modified;
            db.SaveChanges();
            return Json(Common.BuildGeneralResponseJson(true, ResponseCode.Banned, "Successfully banned user."));
        }

        public JsonResult UnbanUser(string loyaltyCardGUID)
        {
            Customer customer = db.Customers.Where(e => e.LoyaltyCardGUID == loyaltyCardGUID).FirstOrDefault();
            customer.IsActive = true;
            customer.IsBanned = false;
            db.Entry(customer).State = System.Data.Entity.EntityState.Modified;
            db.SaveChanges();
            return Json(Common.BuildGeneralResponseJson(true, ResponseCode.RequestFulfilled, "Successfully unbanned user."));
        }

    }
}
