using fekon_repository_api;
using fekon_repository_datamodel.IdentityModels;
using fekon_repository_datamodel.MergeModels;
using fekon_repository_datamodel.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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
        private readonly UserManager<IdentityDataModel> _userManager;
        private readonly SignInManager<IdentityDataModel> _signInManager;

        public UserController(IUserService userService, IWebHostEnvironment webHostEnvironment, UserManager<IdentityDataModel> userManager, SignInManager<IdentityDataModel> signInManager)
        {
            _userService = userService;
            _webHostEnvironment = webHostEnvironment;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        #region USERS
        public async Task<IActionResult> Users(string query, int? pageNumber)
        {
            IQueryable<AspNetUser> data = _userService.GetUsersForPaging(query);
            Dictionary<string, string> routes = new();
            routes.Add("query", query);

            return View(await SearchPaging<AspNetUser>.CreateAsync(data, pageNumber ?? 1, GetDefaultPaging(), routes));
        }
        #endregion

        #region ADMIN
        public async Task<IActionResult> Admin(string query, int? pageNumber)
        {
            IQueryable<AspNetUser> data = _userService.GetAdminForPaging(query);
            ViewData["SearchParameter"] = query;

            Dictionary<string, string> routes = new();
            routes.Add("query", query);

            return View(await SearchPaging<AspNetUser>.CreateAsync(data, pageNumber ?? 1, GetDefaultPaging(), routes));
        }

        public async Task<IActionResult> AdminInformation(string id, int pagenum, bool isredirect = false, string redirectfrom = "")
        {
            bool canLoad = true, canedit = false;
            MergeAdminInfo adminInfo = _userService.GetAdminInfoByIdAsync(id, pagenum == 0 ? 1 : pagenum, ref canLoad);
            if (adminInfo is null)
            {
                return NotFound();
            }

            IdentityDataModel userContext = await _userManager.GetUserAsync(User);
            IList<string> userRole = await _userManager.GetRolesAsync(userContext);
            for (int i = 0; i < userRole.Count; i++)
            {
                if (userRole[i] == "SA" || userContext.Id == id)
                {
                    canedit = true;
                    break;
                }
            }

            ViewData["PageNumber"] = pagenum + 1;
            ViewData["CanLoadMore"] = canLoad;

            if (!canedit)
            {
                ViewData["DisableEdit"] = "disabled";
            }

            if (!isredirect)
                ViewData["ActiveTabTime"] = "active";
            else
                SetActiveTab(redirectfrom);

            return View(adminInfo);
        }

        [Authorize(Roles = "SA")]
        public IActionResult NewAdmin()
        {
            IEnumerable<AspNetRole> listRole = _userService.GetListRole();

            ViewData["ListRole"] = new SelectList(listRole, "Name", "Name");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NewAdmin(MergeNewAdmin mergeNewAdmin)
        {
            if (ModelState.IsValid)
            {
                IdentityDataModel emailExist = await _userManager.FindByEmailAsync(mergeNewAdmin.Email);
                if(emailExist is not null)
                {
                    Notify($"Email {mergeNewAdmin.Email} has been Taken", "Can't Add New Administrator", Models.Common.NotifType.warning);
                    return View(mergeNewAdmin);
                }

                IdentityDataModel newAdmin = new()
                {
                    UserName = mergeNewAdmin.Username,
                    Email = mergeNewAdmin.Email,
                    PhoneNumber = mergeNewAdmin.PhoneNumber
                };

                IdentityResult resultCreate = await _userManager.CreateAsync(newAdmin, mergeNewAdmin.Password);
                if (resultCreate.Succeeded)
                {
                    mergeNewAdmin.RefEmployee.UserId = newAdmin.Id;
                    await _userManager.AddToRoleAsync(newAdmin, mergeNewAdmin.Role);
                    await _userService.CreateNewAdminEmpDataAsync(mergeNewAdmin.RefEmployee);
                    Notify("New Admin has been Added", "Succses On Submit", Models.Common.NotifType.success);
                    return RedirectToAction(nameof(Admin));
                }
                else
                {
                    Notify(resultCreate.Errors.FirstOrDefault().Description, "An Error Occurred", Models.Common.NotifType.error);
                    return View(mergeNewAdmin);
                }
            }
            Notify("Invalid Administrator Data", "An Error Occurred", Models.Common.NotifType.error);
            return View(mergeNewAdmin);
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

            return RedirectToAction(nameof(AdminInformation), new { id = adminInfo.RefEmployee.UserId, isredirect = true, redirectfrom = nameof(EditAdminEmployee) });
        }

        [HttpPost, ActionName("EditAdminUserCredential")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAdminUserCredential(MergeAdminInfo adminInfo)
        {
            IdentityDataModel user = await _userManager.FindByIdAsync(adminInfo.AspNetUser.Id);
            string errorTitle = string.Empty;
            if (adminInfo.AspNetUser.PhoneNumber != user.PhoneNumber)
            {
                IdentityResult setPhoneResult = await _userManager.SetPhoneNumberAsync(user, adminInfo.AspNetUser.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                {
                    Notify("Unable to Change Phone Number", errorTitle = SUBMITERRTITLE, Models.Common.NotifType.error);
                    return RedirectToAction(nameof(AdminInformation), new { user.Id, isredirect = true, redirectfrom = nameof(EditAdminUserCredential) });
                }
            }

            if (adminInfo.AspNetUser.UserName != user.UserName)
            {
                IdentityResult setUsernameResult = await _userManager.SetUserNameAsync(user, adminInfo.AspNetUser.UserName);
                if (!setUsernameResult.Succeeded)
                {
                    Notify(setUsernameResult.Errors.FirstOrDefault().Description, errorTitle = SUBMITERRTITLE, Models.Common.NotifType.error);
                    return RedirectToAction(nameof(AdminInformation), new { user.Id, isredirect = true, redirectfrom = nameof(EditAdminUserCredential) });
                }
            }

            if (adminInfo.AspNetUser.Email.ToLower() != user.Email.ToLower())
            {
                IdentityDataModel emailExist = await _userManager.FindByEmailAsync(adminInfo.AspNetUser.Email);
                if (emailExist is not null)
                {
                    Notify($"Email {adminInfo.AspNetUser.Email} has been Taken", errorTitle = SUBMITERRTITLE, Models.Common.NotifType.warning);
                    return RedirectToAction(nameof(AdminInformation), new { user.Id, isredirect = true, redirectfrom = nameof(EditAdminUserCredential) });
                }
                else
                {
                    IdentityResult setEmailResult = await _userManager.SetEmailAsync(user, adminInfo.AspNetUser.Email);
                    if (!setEmailResult.Succeeded)
                    {
                        Notify(setEmailResult.Errors.FirstOrDefault().Description, errorTitle = SUBMITERRTITLE, Models.Common.NotifType.error);
                        return RedirectToAction(nameof(AdminInformation), new { user.Id, isredirect = true, redirectfrom = nameof(EditAdminUserCredential) });
                    }
                }
            }

            Notify("User Credential has been Edit", "Success on Edit User", Models.Common.NotifType.success);
            string usrCurr = _userManager.GetUserId(User);
            if (usrCurr == user.Id)
                await _signInManager.RefreshSignInAsync(user);

            return RedirectToAction(nameof(AdminInformation), new { user.Id, isredirect = true, redirectfrom = nameof(EditAdminUserCredential) });
        }

        [HttpPost, ActionName("EditNewPassword")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditNewPassword(MergeAdminInfo adminInfo)
        {
            IdentityDataModel user = await _userManager.FindByIdAsync(adminInfo.AspNetUser.Id);
            string errorTitle = string.Empty;

            IdentityResult setPasswordResult = await _userManager.ChangePasswordAsync(user, adminInfo.PasswordChangeInputModel.OldPassword, adminInfo.PasswordChangeInputModel.NewPassword);
            if (!setPasswordResult.Succeeded)
            {
                Notify(setPasswordResult.Errors.FirstOrDefault().Description, errorTitle = SUBMITERRTITLE, Models.Common.NotifType.error);
            }

            if (errorTitle != SUBMITERRTITLE)
            {
                Notify("Password has been Changes", "Success on Update Password", Models.Common.NotifType.success);
                string usrCurr = _userManager.GetUserId(User);
                if(usrCurr == user.Id)
                    await _signInManager.RefreshSignInAsync(user);
            }

            return RedirectToAction(nameof(AdminInformation), new { user.Id, isredirect = true, redirectfrom = nameof(EditNewPassword) });
        }
        #endregion

        private void SetActiveTab(string action)
        {
            switch (action)
            {
                case nameof(EditAdminEmployee):
                    ViewData["ActiveTabEmp"] = "active";
                    break;
                case nameof(EditAdminUserCredential):
                    ViewData["ActiveTabUsr"] = "active";
                    break;
                case nameof(EditNewPassword):
                    ViewData["ActiveTabPass"] = "active";
                    break;
                default:
                    ViewData["ActiveTabTime"] = "active";
                    break;
            }
        }
    }
}
