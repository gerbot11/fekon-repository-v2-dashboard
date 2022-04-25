using fekon_repository_api;
using fekon_repository_datamodel.IdentityModels;
using fekon_repository_datamodel.MergeModels;
using fekon_repository_datamodel.Models;
using fekon_repository_v2_dashboard.Models;
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

        [Authorize(Roles = "SA")]
        public async Task<IActionResult> EditUser(string id)
        {
            IdentityDataModel user = await _userManager.FindByIdAsync(id);
            MergeUserEdit useredit = new()
            {
                UserId = user.Id,
                Username = user.UserName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber
            };

            return View(useredit);
        }

        public async Task<IActionResult> UserDownloadHist(string id, DateTime? datedownload, int pagenumber)
        {
            IEnumerable<MergeUserDownloadHist> userDownloadHist = _userService.GetDownloadUserStatistics(id, datedownload, pagenumber == 0 ? 1 : pagenumber, out bool canLoad);
            IdentityDataModel userAct = await _userManager.FindByIdAsync(id);

            ViewData["PageNumber"] = pagenumber + 1;
            ViewData["CanLoadMore"] = canLoad;
            ViewData["DtActSearch"] = datedownload is not null ? datedownload.Value.ToString("yyyy-MM-dd") : null;
            ViewData["Username"] = userAct.UserName;
            return View(userDownloadHist);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUserCredential(MergeUserEdit useredit)
        {
            IdentityDataModel user = await _userManager.FindByIdAsync(useredit.UserId);
            user.UserName = useredit.Username;
            user.Email = useredit.Email;
            user.PhoneNumber = useredit.PhoneNumber;

            if (await CheckEmailExist(useredit.Email, user))
            {
                Notify($"Email {useredit.Email} already taken", "Error On Update User", Models.Common.NotifType.error);
                return RedirectToAction(nameof(EditUser), new { id = useredit.UserId });
            }

            IdentityResult editResult = await _userManager.UpdateAsync(user);
            if (!editResult.Succeeded)
            {
                Notify(editResult.Errors.FirstOrDefault().Description, "Error On Update User", Models.Common.NotifType.error);
                return RedirectToAction(nameof(EditUser), new { id = useredit.UserId });
            }

            Notify("User Credential has Been Updated", "Update User", Models.Common.NotifType.success);
            return RedirectToAction(nameof(EditUser), new { id = useredit.UserId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUserPassword(MergeUserEdit useredit)
        {
            IdentityDataModel user = await _userManager.FindByIdAsync(useredit.UserId);
            user.PasswordHash = _userManager.PasswordHasher.HashPassword(user, useredit.NewPassword);

            IdentityResult editResult = await _userManager.UpdateAsync(user);
            if (!editResult.Succeeded)
            {
                Notify(editResult.Errors.FirstOrDefault().Description, "Error On Update User", Models.Common.NotifType.error);
            }
            else
            {
                Notify("User Password has Been Updated", "Update User", Models.Common.NotifType.success);
            }

            return RedirectToAction(nameof(EditUser), new { id = useredit.UserId });
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

        public async Task<IActionResult> AdminInformation(string id, int pagenum, DateTime? dateact, bool isredirect = false, string redirectfrom = "")
        {
            bool canLoad = true, canedit = false, forceeditpass = false, caneditrole = false;
            MergeAdminInfo adminInfo = _userService.GetAdminInfoByIdAsync(id, pagenum == 0 ? 1 : pagenum, dateact, ref canLoad);
            if (adminInfo is null)
            {
                return NotFound();
            }

            IdentityDataModel userContext = await _userManager.GetUserAsync(User);
            IList<string> userRole = await _userManager.GetRolesAsync(userContext);
            if (userContext.Id != id)
            {
                for (int i = 0; i < userRole.Count; i++)
                {
                    if (userRole[i] == Common.SA_ROLE_CODE)
                    {
                        canedit = true;
                        forceeditpass = true;
                        caneditrole = true;
                        break;
                    }
                }
            }
            else
            {
                for (int i = 0; i < userRole.Count; i++)
                {
                    if (userRole[i] == Common.SA_ROLE_CODE)
                    {
                        caneditrole = true;
                        break;
                    }
                }
                canedit = true;
            }

            ViewData["PageNumber"] = pagenum + 1;
            ViewData["CanLoadMore"] = canLoad;
            ViewData["CanEdit"] = canedit;
            ViewData["CanEditRole"] = caneditrole;
            ViewData["ForceEditPass"] = forceeditpass;
            ViewData["DtActSearch"] = dateact is not null ? dateact.Value.ToString("yyyy-MM-dd") : null;
            ViewData["DtActSearch2"] = dateact is not null ? dateact.Value : null;
            SetSelectRole(adminInfo.UserRole);

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
            SetSelectRole();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NewAdmin(MergeNewAdmin mergeNewAdmin)
        {
            if (ModelState.IsValid)
            {
                if(await CheckEmailExist(mergeNewAdmin.Email))
                {
                    Notify($"Email {mergeNewAdmin.Email} has been Taken", "Can't Add New Administrator", Models.Common.NotifType.warning);
                    SetSelectRole();
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
                    SetSelectRole();
                    return View(mergeNewAdmin);
                }
            }
            Notify("Invalid Administrator Data", "An Error Occurred", Models.Common.NotifType.error);
            SetSelectRole();
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
        public async Task<IActionResult> EditAdminUserCredential(MergeAdminInfo adminInfo, string selectuserrole)
        {
            IdentityDataModel user = await _userManager.FindByIdAsync(adminInfo.AspNetUser.Id);
            IList<string> userRoles = await _userManager.GetRolesAsync(user);
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
                if (await CheckEmailExist(adminInfo.AspNetUser.Email, user))
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

            if (userRoles.FirstOrDefault() != adminInfo.UserRole)
            {
                await _userManager.RemoveFromRoleAsync(user, adminInfo.UserRole);
                await _userManager.AddToRoleAsync(user, selectuserrole);
            }
             
            Notify("User Credential has been Edit", "Success on Edit User", Common.NotifType.success);
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

            IdentityDataModel currentUserUpdate = await _userManager.GetUserAsync(User);
            IList<string> currUserRole = await _userManager.GetRolesAsync(currentUserUpdate);
            bool isSupAdm = false;

            if(user.Id != currentUserUpdate.Id)
            {
                foreach (string item in currUserRole)
                {
                    if (item == "SA")
                    {
                        isSupAdm = true;
                        break;
                    }
                }
            }

            if (isSupAdm)
            {
                user.PasswordHash = _userManager.PasswordHasher.HashPassword(user, adminInfo.PasswordChangeInputModel.NewPassword);
                IdentityResult setPasswordResult = await _userManager.UpdateAsync(user);
                if (!setPasswordResult.Succeeded)
                {
                    Notify(setPasswordResult.Errors.FirstOrDefault().Description, errorTitle = SUBMITERRTITLE, Models.Common.NotifType.error);
                }
            }
            else
            {
                IdentityResult setPasswordResult = await _userManager.ChangePasswordAsync(user, adminInfo.PasswordChangeInputModel.OldPassword, adminInfo.PasswordChangeInputModel.NewPassword);
                if (!setPasswordResult.Succeeded)
                {
                    Notify(setPasswordResult.Errors.FirstOrDefault().Description, errorTitle = SUBMITERRTITLE, Models.Common.NotifType.error);
                }
            }

            if (errorTitle != SUBMITERRTITLE)
            {
                Notify("Password has been Changes", "Success on Update Password", Models.Common.NotifType.success);
                if(currentUserUpdate.Id == user.Id)
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

        private async Task<bool> CheckEmailExist(string email, IdentityDataModel user = null)
        {
            bool isExist = false;
            IdentityDataModel userEmail = await _userManager.FindByEmailAsync(email);
            if (userEmail is not null)
            {
                if (user is not null)
                {
                    if (user.Id != userEmail.Id)
                    {
                        isExist = true;
                    }
                }
                else
                {
                    isExist = true;
                }
            }

            return isExist;
        }

        private void SetSelectRole(string selected = "")
        {
            IEnumerable<AspNetRole> listRole = _userService.GetListRole();
            ViewData["ListRole"] = new SelectList(listRole, "Name", "Name", string.IsNullOrEmpty(selected) ? null : selected);
        }
    }
}
