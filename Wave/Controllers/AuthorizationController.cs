using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Wave.Models;

namespace Wave.Controllers
{
    public class AuthorizationController : Controller
    {
        // GET: Authorization
        public ActionResult SignUp()
        {
            if(Session["SessionData"] == null)
            {
                Session["SessionData"] = new SessionData();
            }
            ViewBag.SessionData = (SessionData)Session["SessionData"];
            return View();
        }
    }
}