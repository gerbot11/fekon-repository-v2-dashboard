using fekon_repository_api;
using fekon_repository_datamodel.MergeModels;
using fekon_repository_datamodel.Models;
using fekon_repository_v2_dashboard.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace fekon_repository_v2_dashboard.Controllers
{
    public class AuthorController : BaseController
    {
        private readonly IAuthorService _authorService;
        public AuthorController(IAuthorService authorService)
        {
            _authorService = authorService;
        }

        public async Task<IActionResult> Index(string query, int? pageNumber, string isadvisior)
        {
            IQueryable<Author> authors = _authorService.GetAuthorsForIndexDash(query, isadvisior);

            ViewData["SearchParameter"] = query;
            ViewData["IsAdvisior"] = isadvisior;
            return View(await SearchPaging<Author>.CreateAsync(authors, pageNumber ?? 1, GetDefaultPaging()));
        }

        public async Task<IActionResult> Edit(long? id)
        {
            if (id is null)
                return NotFound();

            Author author = await _authorService.GetAuthorByAuthorIdAsync((long)id);

            if (author is null)
                return NotFound();

            SetSelectionIsAdvisior(author.IsAdvisor);
            return View(author);
        }

        public IActionResult Create()
        {
            SetSelectionIsAdvisior(null);
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Author author)
        {
            if (author.IsAdvisor == Common.TRUE_CONDITION || author.IsAdvisor == Common.FALSE_CONDITION)
            {
                string submitresult = await _authorService.AddNewAuthorAsync(author);
                if (string.IsNullOrEmpty(submitresult))
                {
                    Notify("New Author has been Added", SUBMITSUCESSTITLE, Common.NotifType.success);
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    Notify(submitresult, SUBMITERRTITLE, Common.NotifType.error);
                }
            }
            else
            {
                Notify("Please Select Author Contribution", SUBMITERRTITLE, Common.NotifType.error);
            }

            SetSelectionIsAdvisior(null);
            return View(author);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Author author)
        {
            if (author.IsAdvisor == Common.TRUE_CONDITION || author.IsAdvisor == Common.FALSE_CONDITION)
            {
                string resMsg = await _authorService.EditAuthorAsync(author);
                if (string.IsNullOrEmpty(resMsg))
                {
                    Notify("Author Updated Sucessfully", SUBMITSUCESSTITLE, Common.NotifType.success);
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    Notify(resMsg, SUBMITERRTITLE, Common.NotifType.error);
                }
            }
            else
            {
                Notify("Please Select Author Contribution", SUBMITERRTITLE, Common.NotifType.error);
            }

            SetSelectionIsAdvisior(author.IsAdvisor);
            return View(author);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(long id)
        {
            string resMsg = await _authorService.DeleteAuthorAsync(id);
            if (string.IsNullOrEmpty(resMsg))
            {
                Notify("Author has been Deleted Sucesfully", SUBMITSUCESSTITLE, Common.NotifType.success);
            }
            else
            {
                Notify(resMsg, SUBMITERRTITLE, Common.NotifType.error);
            }
            return RedirectToAction(nameof(Index));
        }

        private void SetSelectionIsAdvisior(string selected)
        {
            Dictionary<string, string> list = new();
            list.Add("0", "Penulis");
            list.Add("1", "Pembimbing");
            ViewData["ListIsAdvisiorStat"] = new SelectList(list, "Key", "Value", string.IsNullOrEmpty(selected) ? "x" : selected);
        }
    }
}
