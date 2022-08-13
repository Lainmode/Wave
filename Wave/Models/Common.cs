using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace Wave.Models
{
    public class Common
    {
        private static readonly string accountSid = "AC6be402bc56f34c7e071d91a8a9e71b25";
        private static readonly string authToken = "408f35c09e5acde870efb504bd2ce5b8";

        private static readonly string senderPhoneNumber = "+19379153188";

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


        public static void SendOTPMessage(string phoneNumber, string otp)
        {
            string body = "Your one time password for Doppio is: " + otp;

            TwilioClient.Init(accountSid, authToken);

            var to = new PhoneNumber("+" + phoneNumber);
            var from = new PhoneNumber(senderPhoneNumber);

            MessageResource.Create(
                to: to,
                from: from,
                body: body
                );

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