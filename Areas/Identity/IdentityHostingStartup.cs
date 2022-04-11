using System;
using fekon_repository_datamodel.IdentityModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

[assembly: HostingStartup(typeof(fekon_repository_v2_dashboard.Areas.Identity.IdentityHostingStartup))]
namespace fekon_repository_v2_dashboard.Areas.Identity
{
    public class IdentityHostingStartup : IHostingStartup
    {
        public readonly string DEF_CONSTRING = "RepoAssasins";
        public readonly string DEF_CONSTRING_MYSQL = "FekonConMySql";
        public void Configure(IWebHostBuilder builder)
        {
            builder.ConfigureServices((context, services) => {
                services.AddDbContext<IdentityDataContext>(options =>
                    options.UseMySQL(context.Configuration.GetConnectionString(DEF_CONSTRING_MYSQL)));

                services.AddDefaultIdentity<IdentityDataModel>(options => options.SignIn.RequireConfirmedAccount = false)
                    .AddRoles<IdentityRole>()
                    .AddEntityFrameworkStores<IdentityDataContext>();
            });
        }
    }
}