using fekon_repository_api;
using fekon_repository_datamodel.IdentityModels;
using fekon_repository_datamodel.MergeModels;
using fekon_repository_datamodel.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace fekon_repository_v2_dashboard.Controllers
{
    public class FileMonitoringController : BaseController
    {
        private readonly IFileMonitoringService _fileMonitoringService;
        private readonly IUserService _userService;
        private readonly UserManager<IdentityDataModel> _userManager;

        public FileMonitoringController(IFileMonitoringService fileMonitoringService, IUserService userService, UserManager<IdentityDataModel> userManager)
        {
            _fileMonitoringService = fileMonitoringService;
            _userService = userService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(int? pagenumber,DateTime? dtFrom, DateTime? dtTo)
        {
            IQueryable<FileMonitoringHist> data = _fileMonitoringService.GetMonitoringHistsForPaging(dtFrom, dtTo);
            return View(await SearchPaging<FileMonitoringHist>.CreateAsync(data, pagenumber ?? 1, GetDefaultPaging()));
        }

        public async Task<IActionResult> MonitoringDetail(int? pagenumber, long id)
        {
            IQueryable<FileMonitoringResult> results = _fileMonitoringService.GetFileMonitoringResults(id);
            if (results is null)
                return NotFound();

            return View(await SearchPaging<FileMonitoringResult>.CreateAsync(results, pagenumber ?? 1, GetDefaultPaging()));
        }

        [HttpPost]
        public async Task<IActionResult> RunMonitoring()
        {
            if (_fileMonitoringService.ValidateLastRun(DateTime.Now, GetFileMonitoringNextRun()))
            {
                Notify("Cannot Run This Time", "Error On Monitoring Process", Models.Common.NotifType.warning);
                return RedirectToAction(nameof(Index));
            }
            else
            {
                try
                {
                    DateTime dtStart = DateTime.Now;
                    IEnumerable<FileDetail> listFile = _fileMonitoringService.GetFileDetailsForMonitoring();
                    FileMonitoringHist monitoringHist = _fileMonitoringService.CreateFileMonitoringH(listFile.Count(), listFile.Sum(x => Convert.ToInt32(x.FileSize)));

                    foreach(FileDetail file in listFile)
                    {
                        FileMonitoringResult result = _fileMonitoringService.RunMonitoring(file, monitoringHist);
                        if (result is not null)
                        {
                            monitoringHist.TotalFileProblem++;
                            monitoringHist.FileMonitoringResults.Add(result);
                        }
                    }

                    DateTime dtEnd = DateTime.Now;
                    TimeSpan ts = dtStart - dtEnd;
                    double duration = ts.TotalMinutes;

                    monitoringHist.RunningDuration = Convert.ToInt32(duration);
                    await _fileMonitoringService.UpdateFileMonitoringHist(monitoringHist);
                    await _userService.AddUserActHist(_userManager.GetUserId(User), $"Start Running File Checking on {dtStart}","File Checking");
                }
                catch (Exception ex)
                {
                    string error = ex.InnerException.Message is not null ? ex.InnerException.Message : ex.Message;
                    Notify(error, "Error On Monitoring Process", Models.Common.NotifType.error);
                    return RedirectToAction(nameof(Index));
                }

                Notify("File Monitoring Proces Success", "Monitoring Process Done", Models.Common.NotifType.info);
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        public IActionResult UpdateStatProcess()
        {
            var output = new
            {
                progress = Startup.Progress,
                fname = Startup.FileName,
            };
            return Json(output);
        }
    }
}
