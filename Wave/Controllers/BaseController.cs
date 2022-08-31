using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Wave.Models;

namespace Wave.Controllers
{
    public class BaseController : Controller
    {
        public HttpCookie cookie;
        public WaveEntities db = new WaveEntities();
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            Common.Log(filterContext, db); // Log the request

            if (!Request.Cookies.AllKeys.Contains("WaveSession"))
            {
                filterContext.Result = RedirectToAction("SignUp", "Authorization");
                return;
            }
            cookie = Request.Cookies.Get("WaveSession");
            base.OnActionExecuting(filterContext);
        }

        // GET: Base
        public ActionResult Index()
        {
            return View();
        }
    }
}