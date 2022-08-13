using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Wave.Models
{
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
            if (customer != null)
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
}