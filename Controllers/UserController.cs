using fekon_repository_api;
using fekon_repository_datamodel.MergeModels;
using fekon_repository_datamodel.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace fekon_repository_v2_dashboard.Controllers
{
    public class UserController : BaseController
    {
        private readonly IUserService _userService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public UserController(IUserService userService, IWebHostEnvironment webHostEnvironment)
        {
            _userService = userService;
            _webHostEnvironment = webHostEnvironment;
        }

        #region USERS
        public async Task<IActionResult> Users(string query, int? pageNumber)
        {
            IQueryable<AspNetUser> data = _userService.GetUsersForPaging(query);
            return View(await SearchPaging<AspNetUser>.CreateAsync(data, pageNumber ?? 1, GetDefaultPaging()));
        }
        #endregion

        #region ADMIN
        public async Task<IActionResult> Admin(string query, int? pageNumber)
        {
            IQueryable<AspNetUser> data = _userService.GetAdminForPaging(query);
            return View(await SearchPaging<AspNetUser>.CreateAsync(data, pageNumber ?? 1, GetDefaultPaging()));
        }

        public IActionResult AdminInformation(string id, int pagenum)
        {
            bool canLoad = true;
            MergeAdminInfo adminInfo = _userService.GetAdminInfoByIdAsync(id, pagenum == 0 ? 1 : pagenum, ref canLoad);
             
            if (adminInfo is null)
            {
                return NotFound();
            }

            ViewData["PageNumber"] = pagenum + 1;
            ViewData["CanLoadMore"] = canLoad;
            return View(adminInfo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAdminEmployee(MergeAdminInfo adminInfo, IFormFile files)
        {
            try
            {
                string fileLocation = System.IO.Path.Combine(_webHostEnvironment.WebRootPath, "user_img");
                await _userService.EditRefEmpAsync(adminInfo.RefEmployee, fileLocation, adminInfo.AspNetUser.UserName, files);
                Notify($"Employee {adminInfo.RefEmployee.EmployeeName} Has been Update Succsesfull", "Employee Update", Models.Common.NotifType.success);
            }
            catch (Exception ex)
            {
                Notify(ex.Message, "Error On Update Employee", Models.Common.NotifType.error);
            }
            MergeAdminInfo newAdminInfo = GetAdminInfoForReturn(adminInfo.RefEmployee.UserId);
            //return RedirectToAction(nameof(AdminInformation));
            return View(nameof(AdminInformation), newAdminInfo);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditAdminUserCredential(MergeAdminInfo adminInfo)
        {
            return RedirectToAction(nameof(Admin));
        }
        #endregion

        private MergeAdminInfo GetAdminInfoForReturn(string id)
        {
            bool canLoad = true;
            MergeAdminInfo adminInfo = _userService.GetAdminInfoByIdAsync(id, 1, ref canLoad);

            ViewData["PageNumber"] = 1;
            ViewData["CanLoadMore"] = canLoad;
            return adminInfo;
        }
    }
}
