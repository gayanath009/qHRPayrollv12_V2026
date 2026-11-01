using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using qHRM.Areas.Notify.Models;
using qHRM.Data;

[assembly: HostingStartup(typeof(qHRM.Areas.Notify.NotifyStartup))]
namespace qHRM.Areas.Notify
{
    public class NotifyStartup : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder)
        {
            builder.ConfigureServices((context, services) => {
                services.AddDbContext<NotificationContext>(options =>
                    options.UseSqlServer(
                        context.Configuration.GetConnectionString("NotifyconString")));

                
            });
        }
    }
}