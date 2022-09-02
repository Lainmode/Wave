using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Wave.Models;

namespace Wave.Controllers
{
    public class AuthorizeController : Controller
    {
        // GET: Authorization
        public WaveEntities db = new WaveEntities();
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            Common.Log(filterContext, db); // Log the request

            if (Request.Cookies.AllKeys.Contains("WaveSession"))
            {
                filterContext.Result = Redirect(Url.Action("Index", "Base"));
                return;
            }
            base.OnActionExecuting(filterContext);
        }

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