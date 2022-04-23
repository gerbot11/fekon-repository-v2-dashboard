using fekon_repository_api;
using fekon_repository_datamodel.MergeModels;
using fekon_repository_datamodel.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace fekon_repository_v2_dashboard.Controllers
{
    public class FileTypeController : BaseController
    {
        private readonly IGeneralService _generalService;
        public FileTypeController(IGeneralService generalService)
        {
            _generalService = generalService;
        }

        public IActionResult Index()
        {
            IQueryable<RefRepositoryFileType> data = _generalService.GetRefRepositoryFileTypesPaging();
            MergeRefRepoFileType merge = new()
            {
                PagingData = data
            };
            return View(merge);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MergeRefRepoFileType merge)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(merge.CreateModel.RepositoryFileTypeCode) || string.IsNullOrWhiteSpace(merge.CreateModel.RepositoryFileTypeName))
                {
                    Notify("Type Name and Type Code Cannot Null", SUBMITERRTITLE, Models.Common.NotifType.error);
                }
                else
                {
                    if (_generalService.CheckDuplicateCode(merge.CreateModel.RepositoryFileTypeCode, null))
                    {
                        Notify($"Type Code '{merge.CreateModel.RepositoryFileTypeCode}' Is Already Exist", SUBMITERRTITLE, Models.Common.NotifType.error);
                    }
                    else
                    {
                        await _generalService.CreateNewRefRepositoryFileType(merge.CreateModel);
                        Notify("New Repository File Type Has Been Added", SUBMITSUCESSTITLE, Models.Common.NotifType.success);
                    }
                }
            }
            catch (Exception e)
            {
                Notify(e.Message, SUBMITERRTITLE, Models.Common.NotifType.error);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(MergeRefRepoFileType merge)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(merge.EditModel.RepositoryFileTypeCode) || string.IsNullOrWhiteSpace(merge.EditModel.RepositoryFileTypeName))
                {
                    Notify("Type Name and Type Code Cannot Null", SUBMITERRTITLE, Models.Common.NotifType.error);
                }
                else
                {
                    if (_generalService.CheckDuplicateCode(merge.EditModel.RepositoryFileTypeCode, merge.EditModel.RefRepositoryFileTypeId))
                    {
                        Notify($"Type Code '{merge.EditModel.RepositoryFileTypeCode}' Is Already Exist", SUBMITERRTITLE, Models.Common.NotifType.error);
                        return RedirectToAction(nameof(Index));
                    }
                    await _generalService.EditRefRepositoryFileType(merge.EditModel);
                    Notify("New Repository File Type Has Been Edit", SUBMITSUCESSTITLE, Models.Common.NotifType.success);
                }
            }
            catch (Exception e)
            {
                Notify(e.Message, SUBMITERRTITLE, Models.Common.NotifType.error);
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
