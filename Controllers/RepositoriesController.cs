using fekon_repository_api;
using fekon_repository_datamodel.IdentityModels;
using fekon_repository_datamodel.MergeModels;
using fekon_repository_datamodel.Models;
using fekon_repository_v2_dashboard.Models;
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
    public class RepositoriesController : BaseController
    {
        private readonly IRepoService _repoService;
        private readonly IAuthorService _authorService;
        private readonly ICollectionService _collectionService;
        private readonly ILangService _langService;
        private readonly IPublisherService _publisherService;
        private readonly IGeneralService _generalService;
        private readonly IUserService _userService;
        private readonly UserManager<IdentityDataModel> _userManager;
        public RepositoriesController(IRepoService repoService, IAuthorService authorService, ICollectionService collectionService, 
            ILangService langService, IPublisherService publisherService, IGeneralService generalService, IUserService userService, UserManager<IdentityDataModel> userManager)
        {
            _repoService = repoService;
            _authorService = authorService;
            _collectionService = collectionService;
            _langService = langService;
            _publisherService = publisherService;
            _generalService = generalService;
            _userManager = userManager;
            _userService = userService;
        }

        #region CONTROLLER
        public async Task<IActionResult> Index(string query, int? pageNumber, int? ipp, string searchtype ,long? colltype, long? collD, int? yearfrom, int? yearto, string title, string author)
        {
            if (!string.IsNullOrEmpty(query))
            {
                ViewData["SearchParameter"] = query;
            }

            IQueryable<Repository> repositories;
            if (searchtype == "M")
            {
                repositories = _repoService.MoreSearchRepositoryDashboard(title, author, yearfrom, yearto, colltype, collD);
                ViewData["TitleParam"] = title;
                ViewData["AuthorParam"] = author;
                ViewData["SearchType"] = searchtype;
                ViewData["TypeParam"] = colltype;
                ViewData["SubCollParam"] = collD;
            }
            else
            {
                repositories = _repoService.GetRepositoriesForIndexPageAsync(query);
            }

            await SetCollectionListSearch(colltype);
            SetListYearSearch(yearfrom);

            return View(await SearchPaging<Repository>.CreateAsync(repositories, pageNumber ?? 1, ipp ?? GetDefaultPaging()));
        }

        public async Task<IActionResult> Paging(string query, int? pageNumber, string searchtype, long? colltype, long? collD, int? year, string title, string author)
        {
            if (!string.IsNullOrEmpty(query))
            {
                ViewData["SearchParameter"] = query;
            }

            IQueryable<MergeRepositoryPaging> repositories = _repoService.GetRepositoryPagings(title, author, year, colltype, collD);

            await SetCollectionListSearch(colltype);
            //SetListYearSearch(year);
            return View(await SearchPaging<MergeRepositoryPaging>.CreateAsync(repositories, pageNumber ?? 1, GetDefaultPaging()));
        }

        public async Task<IActionResult> Create()
        {
            await SetViewDataAdd();
            return View();
        }

        public async Task<IActionResult> Edit(long? id)
        {
            if (id is null)
                return NotFound();

            Repository repository = _repoService.GetRepositoryByRepoId((long)id);
            if (repository is null)
                return NotFound();

            List<string> fileStatus = _repoService.CheckFileStatus(repository.FileDetails);
            MergeRepoCreate mergeRepo = new()
            {
                repository = repository,
                fileStatus = fileStatus
            };

            await SetViewDataEdit(repository);

            return View(mergeRepo);
        }

        public IActionResult Detail(long id)
        {
            MergeRepoViewDashboard repository = _repoService.GetRepositoryForDetailById(id);
            if (repository is null)
                return NotFound();

            return View(repository);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MergeRepoCreate merge, List<IFormFile> files)
        {
            string msg = string.Empty, title = string.Empty;
            Common.NotifType notifType;
            if (ModelState.IsValid)
            {
                if (merge.authorIds is not null && merge.advisiorIds is not null)
                {
                    if (merge.langCode is not null)
                    {
                        List<long> listauthors = merge.authorIds.Concat(merge.advisiorIds).ToList();
                        IdentityDataModel user = await _userManager.GetUserAsync(User);
                        merge.repository.UsrCreate = user.Id;
                        msg = await _repoService.CreateNewRepoAsync(merge.repository, files, listauthors, merge.langCode);
                        if (string.IsNullOrEmpty(msg))
                        {
                            msg = "Submit Process is Done";
                            title = SUBMITSUCESSTITLE;
                            notifType = Common.NotifType.success;
                        }
                        else
                        {
                            title = SUBMITERRTITLE;
                            notifType = Common.NotifType.error;
                        }
                    }
                    else
                    {
                        notifType = Common.NotifType.error;
                        title = SUBMITERRTITLE;
                        msg = "Please Select Journal Language";
                    }
                }
                else
                {
                    notifType = Common.NotifType.error;
                    title = SUBMITERRTITLE;
                    msg = "Please Select Authors";
                }
            }
            else
            {
                msg = "Submit Process is Failed, Invalid Input";
                title = SUBMITERRTITLE;
                notifType = Common.NotifType.error;
            }

            Notify(msg, title, notifType);

            if (title == SUBMITSUCESSTITLE)
            {
                return RedirectToAction(nameof(Index));
            }
            else
            {
                await SetViewDataAdd();
                return View(merge);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, MergeRepoCreate merge, List<IFormFile> files)
        {
            if (id != merge.repository.RepositoryId)
                return NotFound();

            string msg, title;
            Common.NotifType notifType;

            if (ModelState.IsValid)
            {
                List<long> authors = merge.authorIds.Concat(merge.advisiorIds).ToList();
                string usrUpd = _userManager.GetUserId(User);
                msg = await _repoService.EditRepoAsync(merge.repository, files, authors, merge.langCode, usrUpd);
                if (string.IsNullOrEmpty(msg))
                {
                    msg = "Repository update process is Done";
                    title = SUBMITSUCESSTITLE;
                    notifType = Common.NotifType.success;
                }
                else
                {
                    title = SUBMITERRTITLE;
                    notifType = Common.NotifType.error;
                }
            }
            else
            {
                msg = "Submit Process is Failed, Invalid Input";
                title = SUBMITERRTITLE;
                notifType = Common.NotifType.error;
            }

            Notify(msg, title, notifType);
            if (title == SUBMITSUCESSTITLE)
                return RedirectToAction(nameof(Index));
            else
            {
                await SetViewDataEdit(merge.repository);
                return View(merge);
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                await _repoService.DeleteRepoAsync(id);
                await _userService.AddUserActHist(_userManager.GetUserId(User), $"Deleting Repository with ID : {id}", "Delete Repository");
                Notify("Delete Process Done", SUBMITSUCESSTITLE, Common.NotifType.success);
            }
            catch (Exception e)
            {
                Notify(e.Message, SUBMITERRTITLE, Common.NotifType.error);
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> CollectRes(long refcollId)
        {
            ViewBag.Collection = new SelectList(await _collectionService.GetCollectionDsByRefCollIdAsync(refcollId), "CollectionDid", "CollectionDname");
            return PartialView("_PartialColl");
        }

        public IActionResult SetYearTo(int yearFromSelected)
        {
            Dictionary<int, string> listYear = new();
            int minYear = yearFromSelected - 1;
            int yearNow = DateTime.Now.Year;
            int dif = yearNow - minYear;

            for (int i = 1; i <= dif; i++)
            {
                int year = minYear + i;
                listYear.Add(year, year.ToString());
            }

            ViewData["YearsTo"] = new SelectList(listYear, "Key", "Value");
            return PartialView("_PartialYearTo");
        }

        [Route("[controller]/[action]/{fname}/{repoid}")]
        public IActionResult OpenFile(string fname, long repoid)
        {
            FileDetail file = _repoService.GetFileDetailByFileName(fname);
            if (file is null)
            {
                Notify("Filedetail Data Not Exist", "Error On Open File", Common.NotifType.error);
                MergeRepoViewDashboard repository = _repoService.GetRepositoryForDetailById(repoid);
                return View(nameof(Detail), repository);
            }

            string fileURL;
            if (file.FilePath.Contains("\\"))
            {
                fileURL = file.FilePath + "\\" + file.FileName;
            }
            else
            {
                fileURL = file.FilePath + "//" + file.FileName;
            }
            
            try
            {
                if (!System.IO.File.Exists(fileURL))
                {
                    Notify("File Not Exist", "Error On Open File", Common.NotifType.error);
                    MergeRepoViewDashboard repository = _repoService.GetRepositoryForDetailById(repoid);
                    return View(nameof(Detail), repository);
                }
                return File(System.IO.File.ReadAllBytes(fileURL), "application/pdf");
            }
            catch (Exception ex)
            {
                Notify(ex.Message, "Error On Open File", Common.NotifType.error);
                MergeRepoViewDashboard repository = _repoService.GetRepositoryForDetailById(repoid);
                return View(nameof(Detail), repository);
            }
        }

        #endregion

        #region PRIVATE METHOD
        private async Task SetViewDataAdd()
        {
            IEnumerable<Author> listAuthor = await _authorService.GetListAuthorForAddRepos();
            IEnumerable<Author> listAdvisor = await _authorService.GetAuthorsAdvisorAsync();
            IEnumerable<RefCollection> listRefColl = await _collectionService.GetRefCollectionsAsyncForAddRepo();

            var atuhors = from l in listAuthor
                          orderby l.FirstName ascending
                          select new
                          {
                              AuthId = l.AuthorId,
                              Name = $"{l.FirstName} {l.LastName}"
                          };

            var advisor = from l in listAdvisor
                          orderby l.FirstName ascending
                          select new
                          {
                              AuthId = l.AuthorId,
                              Name = $"{l.FirstName} {l.LastName}"
                          };

            var refColss = from a in listRefColl
                           select new
                           {
                               TypeId = a.RefCollectionId,
                               TypeName = a.CollName
                           };

            ViewData["AuthorId"] = new SelectList(atuhors, "AuthId", "Name");
            ViewData["Advisior"] = new SelectList(advisor, "AuthId", "Name");
            ViewData["CollType"] = new SelectList(refColss, "TypeId", "TypeName");
            ViewData["Coll"] = new SelectList(await _collectionService.GetCommunitiesAsync(), "CommunityId", "CommunityName");
            ViewData["Lang"] = new SelectList(await _langService.GetRefLanguagesAsyncForAddRepos(), "LangCode", "LangName");
            ViewData["Publisher"] = new SelectList(await _publisherService.GetListPublishersAsync(), "PublisherId", "PublisherName");
        }

        private async Task SetViewDataEdit(Repository repository)
        {
            IEnumerable<Author> listAuthor = await _authorService.GetListAuthorForAddRepos();
            IEnumerable<Author> listAdvisor = await _authorService.GetAuthorsAdvisorAsync();
            IEnumerable<Author> listAuthorRepos = _authorService.GetListAuthorByReposId(repository.RepositoryId);
            IEnumerable<RefCollection> listRefColl = await _collectionService.GetRefCollectionsAsyncForAddRepo();

            string[] listSelectedAuthor = listAuthorRepos.Where(a => a.IsAdvisor == Common.FALSE_CONDITION).Select(a => a.AuthorId.ToString()).ToArray();
            string[] listSelectedAdvisor = listAuthorRepos.Where(a => a.IsAdvisor == Common.TRUE_CONDITION).Select(a => a.AuthorId.ToString()).ToArray();
            string[] listSelectedLang = repository.Language.Split(';').ToArray();

            var atuhors = from l in listAuthor
                          orderby l.FirstName ascending
                          select new
                          {
                              AuthId = l.AuthorId,
                              Name = $"{l.FirstName} {l.LastName}"
                          };

            var advisor = from l in listAdvisor
                          orderby l.FirstName ascending
                          select new
                          {
                              AuthId = l.AuthorId,
                              Name = $"{l.FirstName} {l.LastName}"
                          };

            ViewData["Lang"] = new SelectList(await _langService.GetRefLanguagesAsyncForAddRepos(), "LangCode", "LangName", repository.Language);
            ViewData["Coll"] = new SelectList(listRefColl, "RefCollectionId", "CollName");
            ViewData["AuthorId"] = new SelectList(atuhors, "AuthId", "Name");
            ViewData["Advisior"] = new SelectList(advisor, "AuthId", "Name");
            ViewData["Publisher"] = new SelectList(await _publisherService.GetListPublishersAsync(), "PublisherId", "PublisherName", repository.Publisher);
            ViewData["CommunityEdit"] = new SelectList(await _collectionService.GetCommunitiesAsync(), "CommunityId", "CommunityName", repository.CommunitiyId);
            ViewData["CollectionEdit"] = new SelectList(await _collectionService.GetCollectionDsByRefCollIdAsync((long)repository.RefCollectionId), "CollectionDid", "CollectionDname", repository.CollectionDid);
            ViewData["SelectedAuthor"] = listSelectedAuthor;
            ViewData["SelectedAdvisior"] = listSelectedAdvisor;
            ViewData["SelectedLang"] = listSelectedLang;
        }

        private async Task SetCollectionListSearch(long? type)
        {
            IEnumerable<RefCollection> listRefColl = await _collectionService.GetRefCollectionsAsyncForAddRepo();
            ViewData["RefCollection"] = new SelectList(listRefColl, "RefCollectionId", "CollName", type ?? null);
        }

        private void SetListYearSearch(int? selectedYear)
        {
            Dictionary<int, string> listYear = new();
            int minYear = 2000;
            int yearNow = DateTime.Now.Year;
            int dif = yearNow - minYear;

            for (int i = 1; i <= dif; i++)
            {
                int year = minYear + i;
                listYear.Add(year, year.ToString());
            }
            
            ViewData["Years"] = new SelectList(listYear, "Key", "Value", selectedYear ?? null);
        }
        #endregion
    }
}
