using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;

namespace fekon_repository_v2_dashboard.Models
{
    public static class Common
    {
        public const string TRUE_CONDITION = "1";
        public const string FALSE_CONDITION = "0";
        public const string VISITOR_ROLE_CODE = "VISITOR";
        public const string ADMIN_ROLE_CODE = "ADMIN";
        public const string SA_ROLE_CODE = "SA";
        public enum NotifType
        {
            error,
            success,
            warning,
            info,
            question
        }

        public static string IsActive(this IHtmlHelper html, string control, string action = "")
        {
            Microsoft.AspNetCore.Routing.RouteData routeData = html.ViewContext.RouteData;
            string routeAction = (string)routeData.Values["action"];
            string routeControl = (string)routeData.Values["controller"];

            bool returnActive;
            if (!string.IsNullOrEmpty(action))
            {
                IEnumerable<string> listAction = action.Split(',');
                returnActive = control == routeControl && listAction.Contains(routeAction);
            }
            else
            {
                returnActive = control == routeControl;
            }

            return returnActive ? "active" : string.Empty;
        }

        public static string IsMenuSelected(this IHtmlHelper htmlHelper, string controllers, string cssClass = "menu-open")
        {
            string currentController = htmlHelper.ViewContext.RouteData.Values["Controller"] as string;
            IEnumerable<string> acceptedControllers = (controllers ?? currentController).Split(',');
            return acceptedControllers.Contains(currentController) ? cssClass : string.Empty;
        }
    }
}
