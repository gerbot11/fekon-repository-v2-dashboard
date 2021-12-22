using fekon_repository_api;
using fekon_repository_datamodel.Models;
using fekon_repository_dataservice.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace fekon_repository_v2_dashboard
{
    public class Startup
    {
        public readonly string DEF_CONSTRING = "RepoAssasins";
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();

            //Scope Interface
            services.AddScoped<IRepoService, RepoService>();
            services.AddScoped<IAuthorService, AuthorService>();
            services.AddScoped<ICollectionService, CollectionService>();
            services.AddScoped<ILangService, LangService>();
            services.AddScoped<IPublisherService, PublisherService>();
            services.AddScoped<IDashboardService, DashboardService>();

            services.AddDbContext<REPOSITORY_DEVContext>(op => op.UseSqlServer(Configuration.GetConnectionString(DEF_CONSTRING),
                o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));
            services.AddRouting(op => op.LowercaseUrls = true);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            string path = Directory.GetCurrentDirectory();
            if (!Directory.Exists($"{path}\\Logs"))
            {
                DirectoryInfo di = Directory.CreateDirectory($"{path}\\Logs");
                di.Create();
            }
            loggerFactory.AddFile($"{path}\\Logs\\Log.txt", LogLevel.Error);

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
