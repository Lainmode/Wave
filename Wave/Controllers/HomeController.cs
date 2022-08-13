using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Wave.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            HttpCookie userInfo = new HttpCookie("userInfo");
            userInfo.Expires.Add(new TimeSpan(15000, 0, 0, 0));
            return View();
        }
    }
}