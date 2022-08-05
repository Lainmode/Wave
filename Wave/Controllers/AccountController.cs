using System;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using Wave.Models;

namespace Wave.Controllers
{
    //The authorise tag is deleted for easier access during frontend development !
    public class AccountController : Controller
    {
        public ActionResult PhoneLogin()
        {
            return View();
        }
        public ActionResult VerifySMS()
        {
            return View();
        }
       
    }
}