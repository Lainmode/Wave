using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Wave.Models
{
    public class WaveAuth
    {
    }

    public class WaveAuthData
    {
        public bool IsAdmin { get; set; }
        public bool IsAuthenticated { get; set; }
        public int CustomerID { get; set; }
        public string PhoneNumber { get; set; }
        public int SendOTPAttempts { get; set; }
        public int SubmitOTPAttempts { get; set; }
    }
}