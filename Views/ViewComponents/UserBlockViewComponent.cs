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

namespace fekon_repository_v2_dashboard.Views.ViewComponents
{
    public class UserBlockViewComponent : ViewComponent
    {
        private readonly UserManager<IdentityDataModel> _userManager;
        private readonly IUserService _userService;
        public UserBlockViewComponent(UserManager<IdentityDataModel> userManager, IUserService userService)
        {
            _userManager = userManager;
            _userService = userService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            IdentityDataModel user = await _userManager.GetUserAsync(UserClaimsPrincipal);
            IList<string> roleList = await _userManager.GetRolesAsync(user);
            RefEmployee emp = _userService.GetRefEmployeeObjByUserId(user.Id);
            string role = roleList.FirstOrDefault();

            MergeUserBlock userBlock = new()
            {
                RefEmployee = emp,
                Role = role,
                Username = user.UserName
            };
            return View(userBlock);
        }
    }
}
