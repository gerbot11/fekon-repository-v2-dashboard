using fekon_repository_api;
using fekon_repository_datamodel.MergeModels;
using fekon_repository_datamodel.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace fekon_repository_v2_dashboard.Controllers
{
    public class SubCollController : BaseController
    {
        private readonly ICollectionService _collectionService;
        public SubCollController(ICollectionService collectionService)
        {
            _collectionService = collectionService;
        }
        public async Task<IActionResult> Index(string query)
        {
            IQueryable<CollectionD> collRes = _collectionService.GetSubCollForPaging(query);
            SearchPaging<CollectionD> data = SearchPaging<CollectionD>.CreateFromList(collRes, 1, 100);
            MergeSubCollData dataMerge = new()
            {
                PagingRefCollection = data
            };

            await SetListRefCollection(null);
            return View(dataMerge);
        }

        public async Task<IActionResult> Edit(long id)
        {
            CollectionD subcoll = _collectionService.GetSubCollectionById(id);
            if (subcoll is null)
                return NotFound();

            await SetListRefCollection(subcoll.RefCollectionId);
            return View(subcoll);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(CollectionD collectionD)
        {
            if (collectionD.RefCollectionId is not null)
            {
                string resMsg = _collectionService.AddNewSubColl(collectionD);
                if (!string.IsNullOrEmpty(resMsg))
                {
                    Notify(resMsg, SUBMITERRTITLE, Models.Common.NotifType.error);
                }
                else
                {
                    Notify("New Sub Collection has been Added Succsesfully", SUBMITSUCESSTITLE, Models.Common.NotifType.success);
                }
            }
            else
                Notify("Please Select Collection Type", SUBMITERRTITLE, Models.Common.NotifType.error);

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(CollectionD collectionD)
        {
            string resMsg = _collectionService.EditSubColl(collectionD);
            if (!string.IsNullOrEmpty(resMsg))
            {
                Notify(resMsg, SUBMITERRTITLE, Models.Common.NotifType.error);
                return View(collectionD);
            }
            else
            {
                Notify("New Sub Collection has been Added Succsesfully", SUBMITSUCESSTITLE, Models.Common.NotifType.success);
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(long id)
        {
            string resMsg = _collectionService.DeleteSubcoll(id);
            if (!string.IsNullOrEmpty(resMsg))
            {
                Notify(resMsg, SUBMITERRTITLE, Models.Common.NotifType.error);
            }
            else
            {
                Notify("Sub Collection has been Delete Succsesfully", SUBMITSUCESSTITLE, Models.Common.NotifType.success);
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task SetListRefCollection(long? refcollid)
        {
            IEnumerable<RefCollection> listRefColl = await _collectionService.GetRefCollectionsAsyncForAddRepo();
            ViewData["ListRefColl"] = new SelectList(listRefColl, "RefCollectionId", "CollName", refcollid ?? null);
        }
    }
}
