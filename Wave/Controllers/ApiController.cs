using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
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
        [AllowAnonymous] 
        //[ValidateAntiForgeryToken]
        public async Task<JsonResult> SetOTP(string phoneNumber)
        {
            HttpCookie userInfo = new HttpCookie("userInfo");
            userInfo["PhoneNumber"] = phoneNumber;
            userInfo.Expires.Add(new TimeSpan(15000,0,0,0));
            
                        Random random = new Random();
            if((userInfo["OTPSendRequests"] != null && int.Parse(userInfo["OTPSendRequests"]) < 3) || userInfo["OTPSendRequests"] == null)
            {
                string otp = random.Next(1000, 10000).ToString(); // generates otp between 100k and 1mil (no 000001)
                userInfo["OTP"] = otp;
                userInfo["OTPSendRequests"] = userInfo["OTPSendRequests"] != null ? (int.Parse(userInfo["OTPSendRequests"])  + 1).ToString() : "1";
                // Send OTP to phone code goes here
                await Common.SendOTPMessage(phoneNumber, otp);
                userInfo.Value = AesOperation.EncryptString(AesOperation.key, userInfo.Value);
                Response.Cookies.Add(userInfo);
                return Json(Common.BuildGeneralResponseJson(true, ResponseCode.OTPRequestSuccessful, "OTP Sent!"));
            }
            userInfo.Value = AesOperation.EncryptString(AesOperation.key, userInfo.Value);
            Response.Cookies.Add(userInfo);
            return Json(Common.BuildGeneralResponseJson(false, ResponseCode.ExceededMaximumOTPRequests, "Exceeded maximum tries, please try again in 1 hour."));
        }

        [HttpPost]
        [AllowAnonymous]
        //[ValidateAntiForgeryToken]
        public ActionResult SubmitOTP(string otp)
        {
            HttpCookie userInfo = Request.Cookies["userInfo"];
            string phoneNumber = (string)userInfo["PhoneNumber"];
            if ((userInfo["OTPSubmitTries"] != null && int.Parse(userInfo["OTPSubmitTries"]) < 3) || userInfo["OTPSubmitTries"] == null)
            {
                if (otp == (string)userInfo["OTP"])
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
                    userInfo.Value = AesOperation.EncryptString(AesOperation.key, userInfo.Value);
                    Response.Cookies.Add(userInfo);
                    
                    return Json(Common.BuildGeneralResponseJson(true, ResponseCode.OTPVerificationSuccessful, "Phone number verified!", "http://localhost/User?guid=" + customer.LoyaltyCardGUID));
                }
                userInfo["OTPSubmitTries"] = userInfo["OTPSubmitTries"] != null ? (int.Parse(userInfo["OTPSubmitTries"]) + 1).ToString() : "1";
                userInfo.Value = AesOperation.EncryptString(AesOperation.key, userInfo.Value);
                Response.Cookies.Add(userInfo);
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
                return Json(Common.BuildGeneralResponseJson(false, ResponseCode.GeneralError, "Customer does not exist!"));
            }
        }

        [HttpPost] [Authorize]
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
                return Json(Common.BuildGeneralResponseJson(false, ResponseCode.GeneralError, "New value is greater than the maximum amount allowed."));
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
