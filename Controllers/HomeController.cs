using fekon_repository_api;
using fekon_repository_datamodel.DashboardModels;
using fekon_repository_v2_dashboard.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace fekon_repository_v2_dashboard.Controllers
{
    public class HomeController : BaseController
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IDashboardService _dashboardService;

        public HomeController(ILogger<HomeController> logger, IDashboardService dashboardService)
        {
            _logger = logger;
            _dashboardService = dashboardService;
        }

        public async Task<IActionResult> Index()
        {
            await SetDashboardPerCollection();
            await SetDashboardPerType();
            SetDataPerYear();
            
            SummarySection summarySection = _dashboardService.SetDataSummary();
            return View(summarySection);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private async Task SetDashboardPerCollection()
        {
            IEnumerable<TotalRepositoryPerColl> datacoll = await _dashboardService.SetTotalRepoPerCollection();
            List<string> colorcode = new();

            Dictionary<string, int> keyValuePairs = new();
            Random random = new();

            foreach (var item in datacoll)
            {
                string color = $"#{random.Next(0x1000000):X6}";
                keyValuePairs.Add(item.CollName, item.Data);
                colorcode.Add(color);
            }

            var output = new
            {
                lable = keyValuePairs.Keys,
                data = keyValuePairs.Values,
                color = colorcode
            };

            TempData["DashboardCollection"] = JsonConvert.SerializeObject(output);
        }

        private async Task SetDashboardPerType()
        {
            IEnumerable<TotalRepositoryPerType> datatype = await _dashboardService.SetTotalRepoPerType();
            List<string> colorcode = new();
            Random random = new();

            List<string> typeName = new();
            List<int> count = new();

            foreach (var item in datatype)
            {
                string color = $"#{random.Next(0x1000000):X6}";
                typeName.Add(item.TypeName);
                count.Add(item.Data);
                colorcode.Add(color);
            }

            var output = new
            {
                lable = typeName,
                data = count,
                color = colorcode
            };

            TempData["DashboardType"] = JsonConvert.SerializeObject(output);
        }

        private void SetDataPerYear()
        {
            IEnumerable<TotalRepositoryPerYearPublish> dataYear = _dashboardService.SetTotalRepoPerYearPublishWithSP(GetConnectionString()); ;
            if (dataYear is not null)
            {
                List<string> colorcode = new();
                List<int> count = new();
                List<int> years = new();
                Random random = new();

                int minYear = dataYear.Min(x => x.Year);
                int maxYear = dataYear.Max(x => x.Year);

                int minVal = dataYear.Min(x => x.Value);
                int maxVal = dataYear.Max(x => x.Value);

                foreach (var item in dataYear)
                {
                    string color = $"#{random.Next(0x1000000):X6}";
                    count.Add(item.Value);
                    years.Add(item.Year);
                    colorcode.Add(color);
                }

                var output = new
                {
                    countyear = maxYear - minYear,
                    numconfigmin = minVal,
                    numconfigmax = maxVal,
                    data = count,
                    lable = years
                };
                TempData["DashboardPerYear"] = JsonConvert.SerializeObject(output);
            }
        }
    }
}
