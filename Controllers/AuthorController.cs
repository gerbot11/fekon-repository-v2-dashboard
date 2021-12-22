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
    public class AuthorController : BaseController
    {
        private readonly IAuthorService _authorService;
        public AuthorController(IAuthorService authorService)
        {
            _authorService = authorService;
        }

        public async Task<IActionResult> Index(string query, int? pageNumber)
        {
            IQueryable<Author> authors = _authorService.GetAuthorsForIndexDash(query);
            return View(await SearchPaging<Author>.CreateAsync(authors, pageNumber ?? 1, 10));
        }
    }
}
