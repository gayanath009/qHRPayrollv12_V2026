using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using qHRM.Areas.Identity.Data;
using qHRM.Data;

[assembly: HostingStartup(typeof(qHRM.Areas.Identity.IdentityHostingStartup))]
namespace qHRM.Areas.Identity
{
    public class IdentityHostingStartup : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder)
        {
            builder.ConfigureServices((context, services) => {
                services.AddDbContext<qHRMContext>(options =>
                    options.UseSqlServer(
                        context.Configuration.GetConnectionString("qHRconString")));

                services.AddDefaultIdentity<qHRMUser>(
                    options => options.SignIn.RequireConfirmedAccount = false)
                    .AddEntityFrameworkStores<qHRMContext>();

            });
        }
    }
}