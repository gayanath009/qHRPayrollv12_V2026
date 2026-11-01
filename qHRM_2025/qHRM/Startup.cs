using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using qHRM.Areas.SysAdmin.Data;
using qHRM.Areas.SysAdmin.Models;
using qHRM.Data;
using qHRM.Models;
using qHRM.Area.Config;
using qHRM.Areas.Employee.Data;
using qHRM.Areas.Entity.Data;
using qHRM.Areas.General.Data;
using Newtonsoft.Json.Serialization;
using qHRM.Areas.Leave.Data;
using System.Text.Json;
using qHRM.Areas.Payroll.Data;
using Microsoft.AspNetCore.Http;
using qHRM.Areas.Notify.Data;
using qHRM.Areas.Notify.Models;
using qHRM.Areas.Attendance.Data;
using qHRM.Areas.WorkShift.Data;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using Microsoft.EntityFrameworkCore;
using qHRM.Areas.Integrations.Data;
using qHRM.Areas.Reports.Data;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using qHRM.Areas.Identity.Data;
using qHRM.Extensions;

namespace qHR
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            
            //Version 18.3.0.44                    
            Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("MzQyODIzQDMxMzgyZTMzMmUzME50VktYUVNtRGxXWmxyRUhOY2JXYzNFbGNlRGRYdGNNWlNTdDEzM2F3STA9");
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();
            //Added By Gayanath 29/06/2020 (To Windows Authentication)            
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddHttpContextAccessor();

            // Configure Identity Application Cookie as Default Scheme; Entra ID registered for SSO challenge
            services.AddAuthentication(options =>
            {
                options.DefaultScheme = Microsoft.AspNetCore.Identity.IdentityConstants.ApplicationScheme;
                options.DefaultSignInScheme = Microsoft.AspNetCore.Identity.IdentityConstants.ApplicationScheme;
                options.DefaultChallengeScheme = Microsoft.AspNetCore.Identity.IdentityConstants.ApplicationScheme;
            })
            .AddMicrosoftIdentityWebApp(options =>
            {
                Configuration.Bind("AzureAd", options);
                options.SignInScheme = Microsoft.AspNetCore.Identity.IdentityConstants.ApplicationScheme;
                    options.Events = new Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectEvents
                    {
                        OnTokenValidated = async context =>
                        {
                            var identity = context.Principal?.Identity as System.Security.Claims.ClaimsIdentity;
                            if (identity != null)
                            {
                                var upn = identity.FindFirst("preferred_username")?.Value 
                                          ?? identity.FindFirst(System.Security.Claims.ClaimTypes.Upn)?.Value 
                                          ?? identity.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value 
                                          ?? identity.Name;

                                var loginContext = context.HttpContext.RequestServices.GetRequiredService<LoginContext>();
                                var prvContext = context.HttpContext.RequestServices.GetRequiredService<AdmUserGrpPrvContext>();
                                var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<qHRMUser>>();
                                var signInManager = context.HttpContext.RequestServices.GetRequiredService<SignInManager<qHRMUser>>();

                                var lstUsr = await loginContext.GetLoginSystemAsync("_login", 0, upn, "");
                                if (lstUsr == null || !lstUsr.Any())
                                {
                                    var shortName = upn?.Contains("@") == true ? upn.Split('@')[0] : (upn?.Contains("\\") == true ? upn.Split('\\')[1] : upn);
                                    if (!string.IsNullOrEmpty(shortName))
                                    {
                                        lstUsr = await loginContext.GetLoginSystemAsync("_login", 0, shortName, "");
                                    }
                                }

                                if (lstUsr != null && lstUsr.Any())
                                {
                                    var userObj = lstUsr.First();
                                    var lstPrv = await prvContext.GetAsync("_user_priv_group", userObj.Grp_Id.Value);

                                    var session = context.HttpContext.Session;
                                    foreach (var itm in lstPrv)
                                    {
                                        session.SetString(itm.PrvCode, "y");
                                    }
                                    session.SetInt32("userID", userObj.Usr_Id.Value);
                                    session.SetInt32("userEmpSysId", userObj.Emp_Sys_Id);
                                    session.SetString("userName", userObj.Usr_Name);
                                    session.SetString("userFullName", userObj.Full_Name);
                                    session.SetInt32("userEntId", userObj.Ent_Id.Value);
                                    session.SetString("userPhoto", string.IsNullOrWhiteSpace(userObj.Photo_Name) ? "user.png" : userObj.Photo_Name);
                                    session.SetInt32("userGrpId", userObj.Grp_Id.Value);
                                    session.SetInt32("userDptId", userObj.Dept_Id.Value);
                                    session.SetString("userDashboard", (userObj.Dshbrd_Indx_Txt ?? "").Trim());
                                    session.SetString("mnulUsrLog", "entra");

                                    // Ensure Identity user exists in AspNetUsers for ASP.NET Core Identity authentication
                                    var identityUser = await userManager.FindByNameAsync(userObj.Usr_Name);
                                    if (identityUser == null)
                                    {
                                        identityUser = new qHRMUser
                                        {
                                            UserName = userObj.Usr_Name,
                                            Usr_Id = userObj.Usr_Id.Value,
                                            Emp_Sys_Id = userObj.Emp_Sys_Id,
                                            Email = upn
                                        };
                                        var createResult = await userManager.CreateAsync(identityUser);
                                        if (!createResult.Succeeded)
                                        {
                                            await userManager.CreateAsync(identityUser, Guid.NewGuid().ToString("N") + "Aa1!");
                                        }
                                    }

                                    // Create valid ClaimsPrincipal matching ASP.NET Core Identity scheme
                                    var userPrincipal = await signInManager.CreateUserPrincipalAsync(identityUser);
                                    context.Principal = userPrincipal;
                                }
                                else
                                {
                                    context.Response.Redirect($"/Identity/Account/Login?remoteError={System.Net.WebUtility.UrlEncode($"Account '{upn}' is not registered or authorized in the qHR system.")}");
                                    context.HandleResponse();
                                }
                            }
                        },
                        OnRemoteFailure = context =>
                        {
                            var errorDesc = context.Failure?.Message ?? "Microsoft authentication failed.";
                            context.Response.Redirect($"/Identity/Account/Login?remoteError={System.Net.WebUtility.UrlEncode(errorDesc)}");
                            context.HandleResponse();
                            return Task.CompletedTask;
                        },
                        OnAuthenticationFailed = context =>
                        {
                            var errorDesc = context.Exception?.Message ?? "Microsoft authentication failed.";
                            context.Response.Redirect($"/Identity/Account/Login?remoteError={System.Net.WebUtility.UrlEncode(errorDesc)}");
                            context.HandleResponse();
                            return Task.CompletedTask;
                        }
                    };
                });

            //services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_3_0);

            //Added By Gayanath 30/05/2020 (To Enable Binding Data Via Ajax Calls)
            //Install-Package Microsoft.AspNetCore.Mvc.NewtonsoftJson                   
            //services.AddMvc().AddNewtonsoftJson(options =>
            //{
            //    options.SerializerSettings.ContractResolver = new DefaultContractResolver();
            //});

            services.AddMvc().AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.ContractResolver = new DefaultContractResolver();
            });


            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });


            var configuration = new ConfigurationBuilder()
               .SetBasePath(Directory.GetCurrentDirectory())
               .AddJsonFile("appsettings.json", false)
               .Build();

            //qHR Database
            string conHR = configuration.GetSection("ConnectionStrings").GetSection("qHRconString").Value;
            services.AddDbContext<qHRMasterContext>(options => options.UseSqlServer(conHR));

            //Leave Database
            string conLv = configuration.GetSection("ConnectionStrings").GetSection("lvSvrConString").Value;
            services.AddDbContext<QCSLEAVEContext>(options => options.UseSqlServer(conLv));

            //Notification Database
            string conNotify = configuration.GetSection("ConnectionStrings").GetSection("notifyconstring")?.Value
                               ?? configuration.GetSection("connectionstrings").GetSection("notifyconstring")?.Value;
            if (!string.IsNullOrEmpty(conNotify))
            {
                services.AddDbContext<NotificationContext>(options => options.UseSqlServer(conNotify));
            }


            //-------------Http Context Accessor-------//
            services.AddHttpContextAccessor();

            services.AddScoped(typeof(DbCommitContext<>));

            //-------------Email Configuration --------//
            services.AddScoped<SysConfigContext>();

            //-------------Push Notifications --------//
            //services.AddScoped<PushNotifyData>();

            //-------------User Privileges------------//            
            services.AddScoped<SysAdminContext>();
            services.AddScoped<AdmUserGrpPrvContext>();
            //services.AddScoped<AdmUserContext>();
            services.AddScoped<LoginContext>();


            //------------- Sys Files ---------------//
            services.AddScoped<SysSelectListContext>();
            services.AddScoped<DashboardContext>();

            //------------- Org Masters ------------//
            services.AddScoped<EnumContext>();
            services.AddScoped<EntityContext>();
            services.AddScoped<OrgEmpChartContext>();
            services.AddScoped<OrganizationContext>();

            //------------- Employee ---------------//
            services.AddScoped<EmployeeContext>();
            services.AddScoped<AttnContext>();
            services.AddScoped<WorkShiftContext>();
            //-------------Leave-------------------//
            services.AddScoped<LeaveContext>();
            services.AddScoped<LvIntgrtnContext>();

            //------------- Leave Workflow Integration --------//
            // Named HttpClient for calls to the Dynamic Workflow Engine REST API
            services.AddHttpClient("WorkflowEngine", client =>
            {
                var wfBaseUrl = Configuration["WorkflowEngine:BaseUrl"] ?? "http://localhost:5000";
                client.BaseAddress = new Uri(wfBaseUrl);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.Timeout = TimeSpan.FromSeconds(60);
            });
            services.AddScoped<LeaveWorkflowService>();
            services.AddScoped<LeaveBalanceService>();

            //-------------Payroll----------------//
            services.AddScoped<PayrollContext>();
            services.AddScoped<PrStfLoanContext>();

            //-------------Notification-----------------//
            services.AddScoped<NotifyContext>();
            services.AddScoped<WorkflowNotificationService>();

            //-------------Leave-----------------//
            services.AddScoped<LeaveContext>();

            //-------------Reports-----------------//
            services.AddScoped<ReRptCntxt>();

            //-------------App Warmup Background Service (Cold Start Eliminator)---//
            services.AddHostedService<AppWarmupService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseStatusCodePagesWithRedirects("/Error/{0}");
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseStatusCodePagesWithRedirects("/Error/{0}");
            }
            app.UseStaticFiles();
            app.UseSession(); //Sessions//
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapAreaControllerRoute(
                   name: "Attendance",
                   areaName: "Attendance",
                   pattern: "Attendance/{controller=Attendance}/{action=Index}/{id?}");
                endpoints.MapAreaControllerRoute(
                    name: "Employee",
                    areaName: "Employee",
                    pattern: "Employee/{controller=Employee}/{action=Index}/{id?}");
                endpoints.MapAreaControllerRoute(
                    name: "Entity",
                    areaName: "Entity",
                    pattern: "Entity/{controller=Entity}/{action=Index}/{id?}");
                endpoints.MapAreaControllerRoute(
                   name: "General",
                   areaName: "General",
                   pattern: "General/{controller=General}/{action=Index}/{id?}");
                endpoints.MapAreaControllerRoute(
                   name: "Leave",
                   areaName: "Leave",
                   pattern: "Leave/{controller=Leave}/{action=Index}/{id?}");
                endpoints.MapAreaControllerRoute(
                   name: "Payroll",
                   areaName: "Payroll",
                   pattern: "Payroll/{controller=Payroll}/{action=Index}/{id?}");
                endpoints.MapAreaControllerRoute(
                   name: "SysAdmin",
                   areaName: "SysAdmin",
                   pattern: "SysAdmin/{controller=SysAdmin}/{action=Index}/{id?}");
                endpoints.MapAreaControllerRoute(
                   name: "Config",
                   areaName: "Config",
                   pattern: "Config/{controller=SysConfig}/{action=Index}/{id?}");
                endpoints.MapAreaControllerRoute(
                   name: "Payroll",
                   areaName: "Payroll",
                   pattern: "Payroll/{controller=Payroll}/{action=Index}/{id?}");
                endpoints.MapAreaControllerRoute(
                    name: "WorkShift",
                    areaName: "WorkShift",
                    pattern: "WorkShift/{controller=WorkShift}/{action=Index}/{id?}");
                endpoints.MapAreaControllerRoute(
                   name: "Config",
                   areaName: "Config",
                   pattern: "Config/{controller=SysConfig}/{action=Index}/{id?}");
                // ── Leave Workflow Integration API endpoints ──────────────────
                // Webhook callback from Dynamic Workflow Engine
                endpoints.MapControllerRoute(
                   name: "LvWfCallback",
                   pattern: "api/leaveworkflow/{action}",
                   defaults: new { area = "Leave", controller = "LvWfCallback" });

                // Approver resolution endpoint (called by WF Engine ApiLookup)
                endpoints.MapControllerRoute(
                   name: "LvWfApprovers",
                   pattern: "api/leaveworkflow/approvers/{action}/{empSysId?}/{lvTypId?}",
                   defaults: new { area = "Leave", controller = "LvWfApprover" });
                // ─────────────────────────────────────────────────────────────

                endpoints.MapRazorPages();
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
             
            });            
        }
    }
}
