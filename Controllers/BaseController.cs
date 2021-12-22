using fekon_repository_v2_dashboard.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace fekon_repository_v2_dashboard.Controllers
{
    public class BaseController : Controller
    {
        public readonly string SUBMIT_SUCESS_TITLE = "Submit Succsess";
        public readonly string SUBMIT_ERR_TITLE = "Submit Error";

        public void Notify(string message, string title, Common.NotifType notifType)
        {
            var msg = new
            {
                message,
                title,
                icon = notifType.ToString(),
                type = notifType.ToString()
            };

            TempData["Message"] = JsonConvert.SerializeObject(msg);
        }

        public static int GetDefaultPaging()
        {
            IConfigurationBuilder builder = new ConfigurationBuilder()
                            .SetBasePath(Directory.GetCurrentDirectory())
                            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                            .AddEnvironmentVariables();

            IConfigurationRoot configuration = builder.Build();
            string config = "DefaultItemPerPage";
            string value = configuration[config];
            int.TryParse(value, out int res);

            return res;
        }

        private static string GetProvider()
        {
            IConfigurationBuilder builder = new ConfigurationBuilder()
                            .SetBasePath(Directory.GetCurrentDirectory())
                            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                            .AddEnvironmentVariables();

            IConfigurationRoot configuration = builder.Build();
            string config = "NotificationProvider";
            string value = configuration[config];

            return value;
        }
    }
}
