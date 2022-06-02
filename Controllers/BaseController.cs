using fekon_repository_v2_dashboard.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace fekon_repository_v2_dashboard.Controllers
{
    public class BaseController : Controller
    {
        public const string SUBMITSUCESSTITLE = "Submit Succsess";
        public const string SUBMITERRTITLE = "Submit Error";

        private const string APP_SETTING_FILE_NAME = "appsettings.json";
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
            string value = GetConfigValue("DefaultItemPerPage");
            _ = int.TryParse(value, out int res);
            return res;
        }

        public static string GetConnectionString()
        {
            IConfigurationBuilder builder = new ConfigurationBuilder()
                            .SetBasePath(Directory.GetCurrentDirectory())
                            .AddJsonFile(APP_SETTING_FILE_NAME, optional: false, reloadOnChange: true)
                            .AddEnvironmentVariables();

            IConfigurationRoot configuration = builder.Build();
            string config = configuration.GetConnectionString("FekonConMySql");

            return config;
        }

        public static int GetFileMonitoringNextRun()
        {
            string config = GetConfigValue("FileMonitoringNextRun");
            return System.Convert.ToInt32(config);
        }

        private static string GetConfigValue(string section)
        {
            IConfigurationBuilder builder = new ConfigurationBuilder()
                            .SetBasePath(Directory.GetCurrentDirectory())
                            .AddJsonFile(APP_SETTING_FILE_NAME, optional: false, reloadOnChange: true)
                            .AddEnvironmentVariables();

            IConfigurationRoot configuration = builder.Build();
            string config = configuration[section];
            return config;
        }
    }
}
