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

        public class Common
        {
            private static string infobipApiKey = "f48ba2d5b6a877c612a7cc37884d4b7a-520bb560-ab4e-4f89-bb37-9f47b91b2557";
            private static string infobipUrl = "https://mp62k2.api.infobip.com";
            public static object BuildGeneralResponseJson(bool isSuccess, ResponseCode responseCode, string message)
            {
                return new { success = isSuccess, code = (int)responseCode, message = message, action = Action.Passive };
            }

            public static object BuildGeneralResponseJson(bool isSuccess, ResponseCode responseCode, string message, string redirectUri)
            {
                return new { success = isSuccess, code = (int)responseCode, message = message, action = Action.Redirect, redirectUri = redirectUri };
            }

            public static object BuildDataResponseJson(bool isSuccess, ResponseCode responseCode, string message, string data)
            {
                return new { success = isSuccess, code = (int)responseCode, message, action = Action.Passive, data };
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

            public static Customer AddNewCustomer(string phoneNumber)
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
                db.SaveChanges();
                return customer;
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

            RequestFulfilled = 12,

            // Loyalty concerns
            LoyaltyPointAdded = 15,
            LoyaltyPointsModified = 16,
            LoyaltyPointsReset = 17,

            // idk
            Banned = 80,

            // Errors starting with 2x
            GeneralError = 20,
            ExceededMaximumOTPRequests = 23,
            IncorrectOTP = 24,
            ExceededMaximumOTPSubmissions = 25,

            // Informational
            Information = 45
        }

        public enum Action
        {
            Passive = 0,
            Redirect = 1
        }

        public class AesOperation
        {
            public static string key = "*G-KaPdSgUkXp2s5v8y/B?E(H+MbQeTh";
            public static string EncryptString(string key, string plainText)
            {
                byte[] iv = new byte[16];
                byte[] array;

                using (Aes aes = Aes.Create())
                {
                    aes.Key = Encoding.UTF8.GetBytes(key);
                    aes.IV = iv;

                    ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        using (CryptoStream cryptoStream = new CryptoStream((Stream)memoryStream, encryptor, CryptoStreamMode.Write))
                        {
                            using (StreamWriter streamWriter = new StreamWriter((Stream)cryptoStream))
                            {
                                streamWriter.Write(plainText);
                            }

                            array = memoryStream.ToArray();
                        }
                    }
                }

                return Convert.ToBase64String(array);
            }

            public static string DecryptString(string key, string cipherText)
            {
                byte[] iv = new byte[16];
                byte[] buffer = Convert.FromBase64String(cipherText);

                using (Aes aes = Aes.Create())
                {
                    aes.Key = Encoding.UTF8.GetBytes(key);
                    aes.IV = iv;
                    ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                    using (MemoryStream memoryStream = new MemoryStream(buffer))
                    {
                        using (CryptoStream cryptoStream = new CryptoStream((Stream)memoryStream, decryptor, CryptoStreamMode.Read))
                        {
                            using (StreamReader streamReader = new StreamReader((Stream)cryptoStream))
                            {
                                return streamReader.ReadToEnd();
                            }
                        }
                    }
                }
            }
        }

    }
}
