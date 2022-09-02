using Newtonsoft.Json;
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
                filterContext.Result = Redirect(Url.Action("SignUp", "Authorize"));
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

        [HttpPost]
        public JsonResult RetrieveCustomerInformation()
        {
            string cookieValue = AesOperation.DecryptString(AesOperation.key, cookie.Value);
            Customer customer = db.Customers.Where(e => e.Cookie == cookieValue).FirstOrDefault();
            if (customer != null)
            {
                return Json(Common.BuildDataResponseJson(true, ResponseCode.RequestFulfilled, "Successfully retrieved data.", JsonConvert.SerializeObject(customer)));
            }

            else
            {
                return Json(Common.BuildGeneralResponseJson(false, ResponseCode.GeneralError, "Customer does not exist!"));
            }
        }
    }
}