using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Wave.Models
{
    public class SessionData
    {
        public string PhoneNumber { get; set; }
        public string OTP { get; set; }
        public int RequestOTPAttemps = 0;
        public int SubmitOTPAttempts = 0;
        public bool IsRequestOTPRestricted = false;
        public bool IsSubmitOTPRestricted = false;
        public DateTime RequestOTPCooldown { get; set; }
        public DateTime SubmitOTPCooldown { get; set; }
    }
}