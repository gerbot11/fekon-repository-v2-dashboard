using fekon_repository_api;
using fekon_repository_datamodel.IdentityModels;
using fekon_repository_datamodel.MergeModels;
using fekon_repository_datamodel.Models;
using fekon_repository_v2_dashboard.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
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
        public async Task<IActionResult> Index(string query, int? pageNumber, int? ipp, string searchtype,
            long? colltype, long? collD, int? yearfrom, int? yearto, string title, string author)
        {
            if (!string.IsNullOrEmpty(query))
            {
                ViewData["SearchParameter"] = query;
            }

            IQueryable<Repository> repositories;
            Dictionary<string, string> routes = new();
            if (searchtype == "M")
            {
                repositories = _repoService.MoreSearchRepositoryDashboard(title, author, yearfrom, yearto, colltype, collD);
                routes.Add("searchtype", searchtype);
                routes.Add("query", query);
                routes.Add("colltype", colltype.ToString());
                routes.Add("collD", collD.ToString());
                routes.Add("yearfrom", yearfrom.ToString());
                routes.Add("yearto", yearto.ToString());
                routes.Add("title", title);
                routes.Add("author", author);
            }
            else
            {
                repositories = _repoService.GetRepositoriesForIndexPageAsync(query);
                routes.Add("query", query);
                routes.Add("pageNumber", pageNumber != null ? pageNumber.ToString() : 1.ToString());
            }

            await SetCollectionListSearch(colltype, collD);
            SetListYearSearch(yearfrom);

            return View(await SearchPaging<Repository>.CreateAsync(repositories, pageNumber ?? 1, ipp ?? GetDefaultPaging(), routes));
        }

        public async Task<IActionResult> Paging(string query, int? pageNumber, string searchtype, long? colltype, long? collD, int? year, string title, string author)
        {
            if (!string.IsNullOrEmpty(query))
            {
                ViewData["SearchParameter"] = query;
            }

            IQueryable<MergeRepositoryPaging> repositories = _repoService.GetRepositoryPagings(title, author, year, colltype, collD);

            //await SetCollectionListSearch(colltype);
            //SetListYearSearch(year);
            return View(await SearchPaging<MergeRepositoryPaging>.CreateAsync(repositories, pageNumber ?? 1, GetDefaultPaging()));
        }

        public async Task<IActionResult> Create()
        {
            IEnumerable<RefRepositoryFileType> listFileType = await _generalService.GetRefRepositoryFileTypes();
            MergeRepoCreate merge = new();
            List<RepoFile> repofiles = new();
            foreach (var item in listFileType)
            {
                RepoFile rf = new()
                {
                    FileTypeName = item.RepositoryFileTypeName,
                    FileTypeCode = item.RepositoryFileTypeCode
                };
                repofiles.Add(rf);
            }

            merge.repoFile = repofiles;
            await SetViewDataAdd(null, null);
            return View(merge);
        }

        public async Task<IActionResult> Edit(long? id)
        {
            if (id is null)
                return NotFound();

            Repository repository = _repoService.GetRepositoryByRepoId((long)id);
            if (repository is null)
                return NotFound();

            IEnumerable<RefRepositoryFileType> listFileType = await _generalService.GetRefRepositoryFileTypes();
            List<RepoFile> repofiles = new();
            foreach (var item in listFileType)
            {
                RepoFile rf = new()
                {
                    FileTypeName = item.RepositoryFileTypeName,
                    FileTypeCode = item.RepositoryFileTypeCode,
                    HasFile = _repoService.CheckRepoHasFilePerType(repository.RepositoryId, item.RefRepositoryFileTypeId)
                };
                repofiles.Add(rf);
            }

            IEnumerable<CurrentFileInfo> currentFileInfos = _repoService.GetCurrentFileInfos(repository.RepositoryId);
            MergeRepoCreate mergeRepo = new()
            {
                repository = repository,
                repoFile = repofiles,
                CurrentFileInfos = currentFileInfos
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
        public async Task<IActionResult> Create(MergeRepoCreate merge, List<string> keywords)
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
                        string userId = _userManager.GetUserId(User);
                        merge.repository.UsrCreate = userId;
                        merge.repository.Language = MergeRepositoryLang(merge.langCode);
                        msg = await _repoService.CreateNewRepoAsync(merge.repository, merge.repoFile, listauthors, keywords);

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
                await SetViewDataAdd(merge.repository.RefCollectionId ?? null, merge.repository.CollectionDid ?? null);
                return View(merge);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, MergeRepoCreate merge, List<IFormFile> files, List<string> keywords)
        {
            if (id != merge.repository.RepositoryId)
                return NotFound();

            string msg, title;
            Common.NotifType notifType;

            if (ModelState.IsValid)
            {
                if (merge.langCode is not null)
                {
                    if (merge.authorIds is not null && merge.advisiorIds is not null)
                    {
                        List<long> authors = merge.authorIds.Concat(merge.advisiorIds).ToList();
                        string usrUpd = _userManager.GetUserId(User);
                        merge.repository.Language = MergeRepositoryLang(merge.langCode);

                        msg = await _repoService.EditRepoAsync(merge.repository, merge.repoFile, authors, usrUpd, keywords);
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
                        notifType = Common.NotifType.error;
                        title = SUBMITERRTITLE;
                        msg = "Please Select Authors";
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteFileDetail(long fileid, long repoid)
        {
            string res = _repoService.DeleteRepositoryFile(fileid);
            if (string.IsNullOrEmpty(res))
            {
                Notify("Repository File Has Been Deleted", "Delete Success", Common.NotifType.success);
            }
            else
            {
                Notify(res, "Unable Delete Repository File", Common.NotifType.error);
            }

            return RedirectToAction(nameof(Edit), new { id = repoid });
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
                //MergeRepoViewDashboard repository = _repoService.GetRepositoryForDetailById(repoid);
                return RedirectToAction(nameof(Detail), new { id = repoid});
            }

            string fileURL = System.IO.Path.Combine(file.FilePath, file.FileName);
            
            try
            {
                if (!System.IO.File.Exists(fileURL))
                {
                    Notify("File Not Exist", "Error On Open File", Common.NotifType.error);
                    //MergeRepoViewDashboard repository = _repoService.GetRepositoryForDetailById(repoid);
                    return RedirectToAction(nameof(Detail), new { id = repoid });
                }
                return File(System.IO.File.ReadAllBytes(fileURL), "application/pdf");
            }
            catch (Exception ex)
            {
                Notify(ex.Message, "Error On Open File", Common.NotifType.error);
                //MergeRepoViewDashboard repository = _repoService.GetRepositoryForDetailById(repoid);
                return RedirectToAction(nameof(Detail), new { id = repoid });
            }
        }

        [HttpGet]
        public IActionResult GetResultAuthor(string q)
        {
            IQueryable<Author> listAuthor = _authorService.GetAuthorForSelectionByName(q);
            var atuhors = from l in listAuthor
                          orderby l.FirstName ascending
                          select new
                          {
                              id = l.AuthorId,
                              text = $"{l.FirstName} {l.LastName}"
                          };

            var data = new
            {
                results = atuhors
            };
            
            return Json(data);
        }

        [HttpGet]
        public IActionResult GetResultAdvisior(string q)
        {
            IQueryable<Author> listAuthor = _authorService.GetAdvisiorForSelectionByName(q);
            var atuhors = from l in listAuthor
                          orderby l.FirstName ascending
                          select new
                          {
                              id = l.AuthorId,
                              text = $"{l.FirstName} {l.LastName}"
                          };

            var data = new
            {
                results = atuhors
            };

            return Json(data);
        }

        [HttpGet]
        public IActionResult GetResultKeyword(string q)
        {
            IQueryable<RefKeyword> keywords = _generalService.GetRefKeywords(q);
            var res = from k in keywords
                      orderby k.KeywordName ascending
                      select new
                      {
                          id = k.RefKeywordId,
                          text = k.KeywordName
                      };

            var data = new
            {
                results = res
            };

            return Json(data);
        }
        #endregion

        #region PRIVATE METHOD
        private async Task SetViewDataAdd(long? selectedType, long? selectedColld)
        {
            //IEnumerable<Author> listAuthor = await _authorService.GetListAuthorForAddRepos();
            //IEnumerable<Author> listAdvisor = await _authorService.GetAuthorsAdvisorAsync();
            //IEnumerable<RefCollection> listRefColl = await _collectionService.GetRefCollectionsAsyncForAddRepo();

            //var atuhors = from l in listAuthor
            //              orderby l.FirstName ascending
            //              select new
            //              {
            //                  AuthId = l.AuthorId,
            //                  Name = $"{l.FirstName} {l.LastName}"
            //              };

            //var advisor = from l in listAdvisor
            //              orderby l.FirstName ascending
            //              select new
            //              {
            //                  AuthId = l.AuthorId,
            //                  Name = $"{l.FirstName} {l.LastName}"
            //              };

            //var refColss = from a in listRefColl
            //               select new
            //               {
            //                   TypeId = a.RefCollectionId,
            //                   TypeName = a.CollName
            //               };

            if (selectedType is not null)
                ViewBag.Collection = new SelectList(await _collectionService.GetCollectionDsByRefCollIdAsync((long)selectedType), "CollectionDid", "CollectionDname", selectedColld ?? null);

            //ViewData["ListFileType"] = listFileType;
            //ViewData["Advisior"] = new SelectList(advisor, "AuthId", "Name");
            ViewData["CollType"] = new SelectList(await _collectionService.GetRefCollectionsAsyncForAddRepo(), "RefCollectionId", "CollName", selectedType ?? null);
            ViewData["Coll"] = new SelectList(await _collectionService.GetCommunitiesAsync(), "CommunityId", "CommunityName");
            ViewData["Lang"] = new SelectList(await _langService.GetRefLanguagesAsyncForAddRepos(), "LangCode", "LangName");
            ViewData["Publisher"] = new SelectList(await _publisherService.GetListPublishersAsync(), "PublisherId", "PublisherName");
        }

        private async Task SetViewDataEdit(Repository repository)
        {
            IEnumerable<Author> listAuthorRepos = _authorService.GetListAuthorByReposId(repository.RepositoryId);
            IEnumerable<RefCollection> listRefColl = await _collectionService.GetRefCollectionsAsyncForAddRepo();
            IEnumerable<RefRepositoryFileType> listFileType = await _generalService.GetRefRepositoryFileTypes();
            IEnumerable<RefKeyword> listrefKeywords = await _generalService.GetRepositoryKeywordByRepoId(repository.RepositoryId);

            Dictionary<string, string> listSelectedAuthor = new();
            Dictionary<string, string> listSelectedAdvisor = new();
            Dictionary<string, string> listSelectedKeyword = new();

            foreach (Author item in listAuthorRepos)
            {
                if (item.IsAdvisor == Common.TRUE_CONDITION)
                {
                    listSelectedAdvisor.Add(item.AuthorId.ToString(), $"{item.FirstName} {item.LastName}");
                }
                else
                {
                    listSelectedAuthor.Add(item.AuthorId.ToString(), $"{item.FirstName} {item.LastName}");
                }
            }

            foreach (RefKeyword item in listrefKeywords)
            {
                listSelectedKeyword.Add(item.RefKeywordId.ToString(), item.KeywordName);
            }

            string[] listSelectedLang = string.IsNullOrEmpty(repository.Language) ? null : repository.Language.Split(';').ToArray();

            ViewData["ListFileType"] = listFileType;
            ViewData["ListRepoKeyword"] = listSelectedKeyword;
            ViewData["Lang"] = new SelectList(await _langService.GetRefLanguagesAsyncForAddRepos(), "LangCode", "LangName", repository.Language);
            ViewData["Coll"] = new SelectList(listRefColl, "RefCollectionId", "CollName");
            ViewData["Publisher"] = new SelectList(await _publisherService.GetListPublishersAsync(), "PublisherId", "PublisherName", repository.Publisher);
            ViewData["CommunityEdit"] = new SelectList(await _collectionService.GetCommunitiesAsync(), "CommunityId", "CommunityName", repository.CommunitiyId);
            ViewData["CollectionEdit"] = new SelectList(await _collectionService.GetCollectionDsByRefCollIdAsync((long)repository.RefCollectionId), "CollectionDid", "CollectionDname", repository.CollectionDid);
            ViewData["SelectedAuthor"] = listSelectedAuthor;
            ViewData["SelectedAdvisior"] = listSelectedAdvisor;
            ViewData["SelectedLang"] = listSelectedLang;
        }

        private async Task SetCollectionListSearch(long? type, long? colld)
        {
            IEnumerable<RefCollection> listRefColl = await _collectionService.GetRefCollectionsAsyncForAddRepo();
            ViewData["RefCollection"] = new SelectList(listRefColl, "RefCollectionId", "CollName", type ?? null);

            if (type is not null)
            {
                ViewBag.Collection = new SelectList(await _collectionService.GetCollectionDsByRefCollIdAsync((long)type), "CollectionDid", "CollectionDname", colld ?? null);
            }
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

        private static string MergeRepositoryLang(List<string> langCode)
        {
            string lang = string.Empty;
            for (int i = 0; i < langCode.Count; i++)
            {
                lang = $"{lang}{langCode[i]};";
            }
            lang = lang.Remove(lang.Length - 1, 1);
            
            return lang;
        }

        private bool CheckIsCreateNewKeyword(string keyid)
        {
            if (!long.TryParse(keyid, out long num))
                return true;
            else
                return false;
        }
        #endregion
    }
}
