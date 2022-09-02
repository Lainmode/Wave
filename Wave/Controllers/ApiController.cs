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
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            Common.Log(filterContext, db); // Log the request

            if (Request.Cookies.AllKeys.Contains("WaveSession"))
            {
                filterContext.Result = new JsonResult { Data = Common.BuildGeneralResponseJson(false, ResponseCode.Information, "Redirecting to base", "/Base/Index") };
                return;
            }
            if (Session["SessionData"] == null)
            {
                filterContext.Result = new JsonResult { Data = Common.BuildGeneralResponseJson(false, ResponseCode.Information, "Session expired!", "/Authorize/SignUp") };
                return;
            }
            sessionData = (SessionData)Session["SessionData"];
            base.OnActionExecuting(filterContext);
        }
        // GET: Api
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public JsonResult SetOTP(string phoneNumber)
        {
            if(!Common.IsPhoneNumber(phoneNumber))
            {
                return Json(Common.BuildGeneralResponseJson(false, ResponseCode.GeneralError, "INCORRECT PHONE NUMBER"));
            }
            sessionData.PhoneNumber = phoneNumber;
            if ((sessionData.RequestOTPAttemps < 3 && sessionData.RequestOTPCooldown < DateTime.Now))
            {
                Random random = new Random();
                sessionData.IsRequestOTPRestricted = false;
                string otp = random.Next(1000, 10000).ToString(); // generates otp between 100k and 1mil (no 000001)
                sessionData.OTP = otp;
                sessionData.RequestOTPAttemps++;
                // Send OTP to phone code goes here
                Common.SendOTPMessage(phoneNumber, otp);
                return Json(Common.BuildGeneralResponseJson(true, ResponseCode.OTPRequestSuccessful, "OTP Sent!"));
            }
            else
            {
                if (sessionData.RequestOTPCooldown == new DateTime())
                {
                    sessionData.RequestOTPCooldown = DateTime.Now.AddHours(1);
                }
                sessionData.IsRequestOTPRestricted = true;
                sessionData.RequestOTPAttemps = 0;
            }

            return Json(Common.BuildGeneralResponseJson(false, ResponseCode.ExceededMaximumOTPRequests, "Exceeded maximum tries, please try again in: " + (DateTime.Now - sessionData.RequestOTPCooldown).TotalMinutes));
        }

        [HttpPost]
        [AllowAnonymous]
        //[ValidateAntiForgeryToken]
        public ActionResult SubmitOTP(string otp)
        {
            if (sessionData.SubmitOTPAttempts < 3 && sessionData.SubmitOTPCooldown < DateTime.Now)
            {
                sessionData.IsSubmitOTPRestricted = false;
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
                    return Json(Common.BuildGeneralResponseJson(true, ResponseCode.OTPVerificationSuccessful, "Phone number verified!", Url.Action("Index", "Base")));
                }
                sessionData.SubmitOTPAttempts++;
                return Json(Common.BuildGeneralResponseJson(false, ResponseCode.IncorrectOTP, "Incorrect OTP!"));
            }
            else
            {
                if(sessionData.SubmitOTPCooldown == new DateTime())
                {
                    sessionData.SubmitOTPCooldown = DateTime.Now.AddHours(1);
                }
                sessionData.IsSubmitOTPRestricted = true;
                sessionData.SubmitOTPAttempts = 0;
            }

            return Json(Common.BuildGeneralResponseJson(false, ResponseCode.ExceededMaximumOTPSubmissions, "Exceeded maximum tries, please try again in" + (DateTime.Now - sessionData.SubmitOTPCooldown).TotalMinutes));
        }



    }
}
