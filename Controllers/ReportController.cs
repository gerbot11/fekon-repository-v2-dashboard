using AspNetCore.Reporting;
using fekon_repository_api;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace fekon_repository_v2_dashboard.Controllers
{
    public class ReportController : BaseController
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IRepoService _repoService;
        public ReportController(IWebHostEnvironment webHostEnvironment, IRepoService repoService)
        {
            _webHostEnvironment = webHostEnvironment;
            _repoService = repoService;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["ListYear"] = new SelectList(await _repoService.GetListYearPublishAsync(), "Key", "Value");
            return View();
        }

        [HttpPost]
        public IActionResult Print(int year)
        {
            DataTable dt = _repoService.GetDataReportStatPeryear(year);
            if (dt.Rows.Count < 1)
            {
                Notify("There Is No Data To Print", "Error On Print Report", Models.Common.NotifType.error);
                return RedirectToAction(nameof(Index));
            }

            int extension = 1;
            string path = $"{_webHostEnvironment.ContentRootPath}\\ReportTemplate\\ReportViewAndDownload.rdlc";
            Dictionary<string, string> parameters = new()
            {
                { "yearpublish", year.ToString() }
            };
            LocalReport lr = new(path);
            lr.AddDataSource("DataSet1", dt);
            ReportResult result = lr.Execute(RenderType.Pdf, extension, parameters);
            return File(result.MainStream, "application/pdf");
        }
    }
}
