using fekon_repository_api;
using fekon_repository_datamodel.MergeModels;
using fekon_repository_datamodel.Models;
using fekon_repository_v2_dashboard.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace fekon_repository_v2_dashboard.Controllers
{
    public class CollectionController : BaseController
    {
        private readonly ICollectionService _collectionService;
        public CollectionController(ICollectionService collectionService)
        {
            _collectionService = collectionService;
        }
        public IActionResult Index(string query)
        {
            if (!string.IsNullOrEmpty(query))
            {
                ViewData["SearchParameter"] = query;
            }
            IQueryable<RefCollection> refColl = _collectionService.GetRefCollectionsForPaging(query);
            SearchPaging<RefCollection> data = SearchPaging<RefCollection>.CreateFromList(refColl, 1, GetDefaultPaging());
            MergeCollData collData = new()
            {
                PagingRefCollection = data
            };

            return View(collData);
        }

        public IActionResult Edit(long? id)
        {
            if (id is null)
                return NotFound();

            RefCollection refCollection = _collectionService.GetRefCollectionById((long)id);
            if (refCollection is null)
                return NotFound();

            return View(refCollection);
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public IActionResult Create(RefCollection RefCollection)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    string res = _collectionService.AddNewRefCollection(RefCollection);
                    if (string.IsNullOrEmpty(res))
                    {
                        Notify("New Collection Type Has Been Added", SUBMITSUCESSTITLE, Models.Common.NotifType.success);
                    }
                    else
                    {
                        Notify(res, SUBMITERRTITLE, Models.Common.NotifType.error);
                    }
                } 
                catch(Exception ex)
                {
                    Notify(ex.Message, SUBMITERRTITLE, Models.Common.NotifType.error);
                }
            }
            return RedirectToAction(nameof(Index));
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public IActionResult Edit(RefCollection RefCollection)
        {
            if (ModelState.IsValid)
            {
                string res = _collectionService.EditRefCollection(RefCollection);
                if (string.IsNullOrEmpty(res))
                {
                    Notify("Collection Type Has Benn Edit Sucsessfull", SUBMITSUCESSTITLE, Models.Common.NotifType.success);
                }
                else
                {
                    Notify(res, SUBMITERRTITLE, Models.Common.NotifType.error);
                    return View(RefCollection);
                }
            }
            return RedirectToAction(nameof(Index));
        }

        [ValidateAntiForgeryToken]
        [HttpPost]
        public IActionResult Delete(long id)
        {
            string resMsg = _collectionService.DeleteRefCollection(id);
            if (string.IsNullOrEmpty(resMsg))
                Notify("Collection Deleted Succsesfully", SUBMITSUCESSTITLE, Common.NotifType.success);
            else
                Notify(resMsg, SUBMITERRTITLE, Common.NotifType.error);

            return RedirectToAction(nameof(Index));
        }
    }
}
