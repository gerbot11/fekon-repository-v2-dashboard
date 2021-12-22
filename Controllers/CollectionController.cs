using fekon_repository_api;
using fekon_repository_datamodel.MergeModels;
using fekon_repository_datamodel.Models;
using Microsoft.AspNetCore.Mvc;
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
        public async Task<IActionResult> Index(string query, int? pageNumber)
        {
            if (!string.IsNullOrEmpty(query))
            {
                ViewData["SearchParameter"] = query;
            }
            IQueryable<RefCollection> refColl = _collectionService.GetRefCollectionsForPaging(query);
            return View(await SearchPaging<RefCollection>.CreateAsync(refColl, pageNumber ?? 1, 25));
        }
    }
}
