using fekon_repository_api;
using fekon_repository_datamodel.MergeModels;
using fekon_repository_datamodel.Models;
using fekon_repository_v2_dashboard.Models;
using Microsoft.AspNetCore.Http;
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
        public RepositoriesController(IRepoService repoService, IAuthorService authorService, ICollectionService collectionService, ILangService langService, IPublisherService publisherService)
        {
            _repoService = repoService;
            _authorService = authorService;
            _collectionService = collectionService;
            _langService = langService;
            _publisherService = publisherService;
        }

        #region CONTROLLER
        public async Task<IActionResult> Index(string query, int? pageNumber)
        {
            if (!string.IsNullOrEmpty(query))
            {
                ViewData["SearchParameter"] = query;
            }

            IQueryable<Repository> repositories = _repoService.GetRepositoriesForIndexPageAsync(query);
            return View(await SearchPaging<Repository>.CreateAsync(repositories, pageNumber ?? 1, 25));
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MergeRepoCreate merge, List<IFormFile> files, List<long> authorIds)
        {
            string msg, title;
            Common.NotifType notifType;
            if (ModelState.IsValid)
            {
                msg = await _repoService.CreateNewRepoAsync(merge.repository, files, authorIds);
                if (string.IsNullOrEmpty(msg))
                {
                    msg = "Submit Process is Done";
                    title = SUBMIT_SUCESS_TITLE;
                    notifType = Common.NotifType.success;
                }
                else
                {
                    title = SUBMIT_ERR_TITLE;
                    notifType = Common.NotifType.error;
                }
            }
            else
            {
                msg = "Submit Process is Failed, Invalid Input";
                title = SUBMIT_ERR_TITLE;
                notifType = Common.NotifType.error;
            }
            Notify(msg, title, notifType);

            if (title == SUBMIT_SUCESS_TITLE)
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
        public async Task<IActionResult> Edit(long id, MergeRepoCreate merge, List<IFormFile> files, List<long> authorIds)
        {
            if (id != merge.repository.RepositoryId)
                return NotFound();

            string msg, title;
            Common.NotifType notifType;

            if (ModelState.IsValid)
            {
                msg = await _repoService.EditRepoAsync(merge.repository, files, authorIds);
                if (string.IsNullOrEmpty(msg))
                {
                    msg = "Repository update process is Done";
                    title = SUBMIT_SUCESS_TITLE;
                    notifType = Common.NotifType.success;
                }
                else
                {
                    title = SUBMIT_ERR_TITLE;
                    notifType = Common.NotifType.error;
                }
            }
            else
            {
                msg = "Submit Process is Failed, Invalid Input";
                title = SUBMIT_ERR_TITLE;
                notifType = Common.NotifType.error;
            }

            Notify(msg, title, notifType);
            if (title == SUBMIT_SUCESS_TITLE)
                return RedirectToAction(nameof(Index));
            else
            {
                await SetViewDataEdit(merge.repository);
                return View(merge);
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(long id)
        {
            try
            {
                _repoService.DeleteRepoAsync(id);
                Notify("Delete Process Done", SUBMIT_SUCESS_TITLE, Common.NotifType.success);
            }
            catch(Exception e)
            {
                Notify(e.Message, SUBMIT_ERR_TITLE, Common.NotifType.success);
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> CollectRes(long refcollId)
        {
            ViewBag.Collection = new SelectList(await _collectionService.GetCollectionDsByRefCollIdAsync(refcollId), "CollectionDid", "CollectionDname");
            return PartialView("_PartialColl");
        }

        #endregion

        #region PRIVATE METHOD
        private async Task SetViewDataAdd()
        {
            IEnumerable<Author> listAuthor = await _authorService.GetListAuthorForAddRepos();
            IEnumerable<RefCollection> listRefColl = await _collectionService.GetRefCollectionsAsyncForAddRepo();

            var atuhors = from l in listAuthor
                          orderby l.FirstName ascending
                          select new
                          {
                              AuthId = l.AuthorId,
                              Name = l.FirstName + " " + l.LastName
                          };

            var refColss = from a in listRefColl
                           select new
                           {
                               TypeId = a.RefCollectionId,
                               TypeName = a.CollName
                           };

            ViewData["AuthorId"] = new SelectList(atuhors, "AuthId", "Name");
            ViewData["CollType"] = new SelectList(refColss, "TypeId", "TypeName");
            ViewData["Coll"] = new SelectList(await _collectionService.GetCommunitiesAsync(), "CommunityId", "CommunityName");
            ViewData["Lang"] = new SelectList(await _langService.GetRefLanguagesAsyncForAddRepos(), "LangCode", "LangName");
            ViewData["Publisher"] = new SelectList(await _publisherService.GetListPublishersAsync(), "PublisherId", "PublisherName");
        }

        private async Task SetViewDataEdit(Repository repository)
        {
            IEnumerable<Author> listAuthor = await _authorService.GetListAuthorForAddRepos();
            IEnumerable<RefCollection> listRefColl = await _collectionService.GetRefCollectionsAsyncForAddRepo();

            string[] listSelectedAuthor = _authorService.GetListAuthorByReposId(repository.RepositoryId).Select(a => a.AuthorId.ToString()).ToArray();

            var atuhors = from l in listAuthor
                          orderby l.FirstName ascending
                          select new
                          {
                              AuthId = l.AuthorId,
                              Name = l.FirstName + " " + l.LastName
                          };

            ViewData["Lang"] = new SelectList(await _langService.GetRefLanguagesAsyncForAddRepos(), "LangCode", "LangName", repository.Language);
            ViewData["Coll"] = new SelectList(listRefColl, "RefCollectionId", "CollName");
            ViewData["AuthorId"] = new SelectList(atuhors, "AuthId", "Name");
            ViewData["Publisher"] = new SelectList(await _publisherService.GetListPublishersAsync(), "PublisherId", "PublisherName", repository.Publisher);
            ViewData["CommunityEdit"] = new SelectList(await _collectionService.GetCommunitiesAsync(), "CommunityId", "CommunityName", repository.CommunitiyId);
            ViewData["CollectionEdit"] = new SelectList(await _collectionService.GetCollectionDsByRefCollIdAsync((long)repository.RefCollectionId), "CollectionDid", "CollectionDname", repository.CollectionDid);
            ViewData["SelectedAuthor"] = listSelectedAuthor;
        }
        #endregion
    }
}
