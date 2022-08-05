using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Wave.Controllers
{
    public class ApiController : Controller
    {
        // GET: Api
        [HttpPost]
        public JsonResult SetOTP(string phoneNumber)
        {
            Random random = new Random();
            if((Session["OTPSendRequests"] != null && (int)Session["OTPSendRequests"] < 3) || Session["OTPSendRequests"] == null)
            {
                string otp = random.Next(100000, 1000000).ToString(); // generates otp between 100k and 1mil (no 000001)
                Session["OTP"] = otp;
                Session["OTPSendRequests"] = Session["OTPSendRequests"] != null ? (int)Session["OTPSendRequests"] + 1 : 1;
                // Send OTP to phone code goes here
                return Json(new { phoneNumber = phoneNumber, success = true, otp = otp, tries = 3 - (int)Session["OTPSendRequests"] });
                return Json(Common.BuildGeneralResponseJson(true, ResponseCode.OTPRequestSuccessful, "OTP Sent!"));
            }

            return Json(Common.BuildGeneralResponseJson(false, ResponseCode.ExceededMaximumOTPRequests, "Exceeded maximum tries, please try again in 1 hour."));


        }

        [HttpPost]
        public ActionResult SubmitOTP(string otp)
        {
            if ((Session["OTPSubmitTries"] != null && (int)Session["OTPSubmitTries"] < 3) || Session["OTPSubmitTries"] == null)
            {
                if (otp == (string)Session["OTP"])
                {
                    return Json(Common.BuildGeneralResponseJson(true, ResponseCode.OTPVerificationSuccessful, "Phone number verified!"));
                }
                Session["OTPSubmitTries"] = Session["OTPSubmitTries"] != null ? (int)Session["OTPSubmitTries"] + 1 : 1;
                return Json(Common.BuildGeneralResponseJson(false, ResponseCode.IncorrectOTP, "Incorrect OTP!"));
            }
            return Json(Common.BuildGeneralResponseJson(false, ResponseCode.ExceededMaximumOTPSubmissions, "Exceeded maximum tries, please try again in 1 hour."));
        }

        public class Common
        {
            public static object BuildGeneralResponseJson(bool isSuccess, ResponseCode responseCode, string message)
            {
                return new { success = isSuccess, code = (int)responseCode, message = message };
            }
        }

        public enum ResponseCode
        {
            // Successes starting with 1x
            OTPRequestSuccessful = 10,
            OTPVerificationSuccessful = 11,

            // Errors starting with 2x
            UnknownError = 20,
            ExceededMaximumOTPRequests = 23,
            IncorrectOTP = 24,
            ExceededMaximumOTPSubmissions = 25
        }

    }
}
