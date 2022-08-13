using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using Wave.Models;

namespace Wave.Controllers
{
    public class ApiController : Controller
    {
        // GET: Api
        [HttpPost]
        public async Task<JsonResult> SetOTP(string phoneNumber)
        {
            Session["PhoneNumber"] = phoneNumber;
            Random random = new Random();
            if((Session["OTPSendRequests"] != null && (int)Session["OTPSendRequests"] < 3) || Session["OTPSendRequests"] == null)
            {
                string otp = random.Next(1000, 10000).ToString(); // generates otp between 100k and 1mil (no 000001)
                Session["OTP"] = otp;
                Session["OTPSendRequests"] = Session["OTPSendRequests"] != null ? (int)Session["OTPSendRequests"]  + 1 : 1;
                // Send OTP to phone code goes here
                await Common.SendOTPMessage(phoneNumber, otp);
                return Json(Common.BuildGeneralResponseJson(true, ResponseCode.OTPRequestSuccessful, "OTP Sent!"));
            }

            return Json(Common.BuildGeneralResponseJson(false, ResponseCode.ExceededMaximumOTPRequests, "Exceeded maximum tries, please try again in 1 hour."));
        }

        [HttpPost]
        public ActionResult SubmitOTP(string otp)
        {
            string phoneNumber = (string)Session["PhoneNumber"];
            if ((Session["OTPSubmitTries"] != null && (int)Session["OTPSubmitTries"] < 3) || Session["OTPSubmitTries"] == null)
            {
                if (otp == (string)Session["OTP"])
                {
                    // register phone here and create all the stuff uknow 
                    // incomplete it does absolutely nothing rn
                    Customer customer;
                    if (!Common.CheckIfPhoneNumberAlreadyRegistered(phoneNumber)) 
                    {
                        customer = Common.AddNewCustomer(phoneNumber);
                        FormsAuthentication.SetAuthCookie(phoneNumber, true);
                    }
                    else
                    {
                        WaveEntities db = new WaveEntities();
                        customer = db.Customers.Where(e => e.PhoneNumber == phoneNumber).FirstOrDefault();
                        FormsAuthentication.SetAuthCookie(phoneNumber, true);
                    }
                    return Json(Common.BuildGeneralResponseJson(true, ResponseCode.OTPVerificationSuccessful, "Phone number verified!", "http://localhost/User?guid=" + customer.LoyaltyCardGUID));
                }
                Session["OTPSubmitTries"] = Session["OTPSubmitTries"] != null ? (int)Session["OTPSubmitTries"] + 1 : 1;
                return Json(Common.BuildGeneralResponseJson(false, ResponseCode.IncorrectOTP, "Incorrect OTP!"));
            }
            return Json(Common.BuildGeneralResponseJson(false, ResponseCode.ExceededMaximumOTPSubmissions, "Exceeded maximum tries, please try again in 1 hour."));
        }

        [HttpPost]
        public JsonResult RetrieveCustomerInformation(string loyaltyCardGUID)
        {
            WaveEntities db = new WaveEntities();
            Customer customer = db.Customers.Where(e => e.LoyaltyCardGUID == loyaltyCardGUID).FirstOrDefault();
            if(customer != null)
            {
                return Json(Common.BuildDataResponseJson(true, ResponseCode.RequestFulfilled, "Successfully retrieved data.", JsonConvert.SerializeObject(customer)));
            }

            else
            {
                return Json(Common.BuildGeneralResponseJson(false, ResponseCode.UnknownError, "Customer does not exist!"));
            }
        }

        [HttpPost]
        public JsonResult ModifyLoyaltyPoints(string loyaltyCardGUID, int newValue)
        {
            if(newValue <= 6)
            {
                WaveEntities db = new WaveEntities();
                Customer customer = db.Customers.Where(e => e.LoyaltyCardGUID == loyaltyCardGUID).FirstOrDefault();
                customer.LoyaltyPoints = newValue;
                customer.Scans.Add(new Scan() { ScanDate = DateTime.Now });
                db.Entry(customer).State = System.Data.Entity.EntityState.Modified;
                db.SaveChanges();
                return Json(Common.BuildGeneralResponseJson(true, ResponseCode.LoyaltyPointsModified, "Successfully modified loyalty points."));
            }
            else
            {
                return Json(Common.BuildGeneralResponseJson(false, ResponseCode.UnknownError, "New value is greater than the maximum amount allowed."));
            }
        }

        public JsonResult BanUser(string loyaltyCardGUID)
        {
            WaveEntities db = new WaveEntities();
            Customer customer = db.Customers.Where(e => e.LoyaltyCardGUID == loyaltyCardGUID).FirstOrDefault();
            customer.IsActive = false;
            customer.IsBanned = true;
            db.Entry(customer).State = System.Data.Entity.EntityState.Modified;
            db.SaveChanges();
            return Json(Common.BuildGeneralResponseJson(true, ResponseCode.Banned, "Successfully banned user."));
        }

        public JsonResult UnbanUser(string loyaltyCardGUID)
        {
            WaveEntities db = new WaveEntities();
            Customer customer = db.Customers.Where(e => e.LoyaltyCardGUID == loyaltyCardGUID).FirstOrDefault();
            customer.IsActive = true;
            customer.IsBanned = false;
            db.Entry(customer).State = System.Data.Entity.EntityState.Modified;
            db.SaveChanges();
            return Json(Common.BuildGeneralResponseJson(true, ResponseCode.RequestFulfilled, "Successfully unbanned user."));
        }
    }
}
