using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;

namespace EgitimPrj.Filters
{
    public sealed class ParentAuthorizeAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var http = context.HttpContext;
            var isParentLoggedIn = http.Session.GetString("IsParentLoggedIn");
            var token = http.Session.GetString("ParentToken");

            if (!string.Equals(isParentLoggedIn, "true", StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(token))
            {
                context.Result = new RedirectToActionResult("Login", "Parent", null);
                return;
            }

            base.OnActionExecuting(context);
        }
    }
}

