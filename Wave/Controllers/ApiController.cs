﻿using System;
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
                    if (!Common.CheckIfPhoneNumberAlreadyRegistered(phoneNumber)) 
                    {
                        Common.AddNewCustomer(phoneNumber);
                        FormsAuthentication.SetAuthCookie(phoneNumber, true);
                    }
                    else
                    {
                        FormsAuthentication.SetAuthCookie(phoneNumber, true);
                    }
                    return Json(Common.BuildGeneralResponseJson(true, ResponseCode.OTPVerificationSuccessful, "Phone number verified!", "http://localhost/Home/Index"));
                }
                Session["OTPSubmitTries"] = Session["OTPSubmitTries"] != null ? (int)Session["OTPSubmitTries"] + 1 : 1;
                return Json(Common.BuildGeneralResponseJson(false, ResponseCode.IncorrectOTP, "Incorrect OTP!"));
            }
            return Json(Common.BuildGeneralResponseJson(false, ResponseCode.ExceededMaximumOTPSubmissions, "Exceeded maximum tries, please try again in 1 hour."));
        }

        public class Common
        {
            private static string infobipApiKey = "f48ba2d5b6a877c612a7cc37884d4b7a-520bb560-ab4e-4f89-bb37-9f47b91b2557";
            private static string infobipUrl = "https://mp62k2.api.infobip.com";
            public static object BuildGeneralResponseJson(bool isSuccess, ResponseCode responseCode, string message)
            {
                return new { success = isSuccess, code = (int)responseCode, message = message };
            }

            public static object BuildGeneralResponseJson(bool isSuccess, ResponseCode responseCode, string message, string redirectUri)
            {
                return new { success = isSuccess, code = (int)responseCode, message = message, redirectUri = redirectUri };
            }


            public static async Task SendOTPMessage(string phoneNumber, string otp)
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("App", infobipApiKey);
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    string message = $@"
                    {{
                        ""messages"": [
                        {{
                            ""from"": ""Doppio Test"",
                            ""destinations"":
                            [
                                {{
                                    ""to"": ""{phoneNumber}""
                                }}
                          ],
                          ""text"": ""Your one time password for Doppio is: {otp}""
                        }}
                      ]
                    }}";

                    var content = new StringContent(message, Encoding.UTF8, "application/json");

                    var response = await httpClient.PostAsync(infobipUrl + "/sms/2/text/advanced", content);
                    var responseContent = await response.Content.ReadAsStringAsync();

                }

            }

            public static bool CheckIfPhoneNumberAlreadyRegistered(string phoneNumber)
            {
                WaveEntities db = new WaveEntities();
                var customer = db.Customers.Where(c => c.PhoneNumber == phoneNumber).FirstOrDefault();
                if(customer != null)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            public static void AddNewCustomer(string phoneNumber)
            {
                WaveEntities db = new WaveEntities();
                Customer customer = new Customer()
                {
                    PhoneNumber = phoneNumber,
                    IsActive = true,
                    IsBanned = false,
                    LoyaltyPoints = 0,
                    LoyaltyCardGUID = GenerateShortGuid(),
                };
                db.Entry(customer).State = System.Data.Entity.EntityState.Added;
            }

            public static string GenerateShortGuid()
            {
                string modifiedBase64 = Convert.ToBase64String((Guid.NewGuid().ToByteArray()))
                    .Replace('+', '-').Replace('/', '_')
                    .Substring(0, 22);
                return modifiedBase64;
            }
        }

        public enum ResponseCode
        {
            // Successes starting with 1x
            OTPRequestSuccessful = 10,
            OTPVerificationSuccessful = 11,

            // Loyalty concerns
            LoyaltyPointAdded = 15,
            LoyaltyPointsModified = 16,
            LoyaltyPointsReset = 17,

            // idk
            Banned = 80,

            // Errors starting with 2x
            UnknownError = 20,
            ExceededMaximumOTPRequests = 23,
            IncorrectOTP = 24,
            ExceededMaximumOTPSubmissions = 25,

            // Informational
            Information = 45
        }

    }
}
