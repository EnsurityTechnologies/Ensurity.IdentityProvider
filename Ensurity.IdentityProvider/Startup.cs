using Ensurity.IdentityProvider.Data;
using Ensurity.MultiTenancyServer;
using IdentityModel.Client;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Mappers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Ensurity.IdentityProvider
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(
                    Configuration.GetConnectionString("DefaultConnection")));
            services.AddDatabaseDeveloperPageExceptionFilter();
            services.AddIdentity<IdentityUser,IdentityRole>(options => options.SignIn.RequireConfirmedAccount = false)
                .AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();

            services.AddTransient<IEmailSender, EmailSender>();

            var migrationsAssembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;


            #region   Identity Server Start

            IIdentityServerBuilder ids = services.AddIdentityServer(options =>
            {
                options.UserInteraction.ConsentUrl = "/Consent";
                options.UserInteraction.LoginUrl = "/Identity/Account/Login";
                options.UserInteraction.LogoutUrl = "/Identity/Account/Logout";
            })
             .AddDeveloperSigningCredential();


            // EF client, scope, and persisted grant stores
            ids.AddOperationalStore<ApplicationDbContext>(options =>
                    options.ConfigureDbContext = builder =>
                        builder.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"), sqlOptions => sqlOptions.MigrationsAssembly(migrationsAssembly)))
                .AddConfigurationStore<ApplicationDbContext>(options =>
                    options.ConfigureDbContext = builder =>
                        builder.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"), sqlOptions => sqlOptions.MigrationsAssembly(migrationsAssembly)));

            // ASP.NET Identity integration
            ids.AddAspNetIdentity<IdentityUser>();


            #endregion -----------------




            services.AddMultiTenancy<TenancyTenant, string>()
               // To test a domain parser locally, add a similar line 
               // to your hosts file for each tenant you want to test
               // For Windows: C:\Windows\System32\drivers\etc\hosts
               // 127.0.0.1	tenant1.tenants.local
               // 127.0.0.1	tenant2.tenants.local
               .AddSubdomainParser(".ensurity.com")
               .AddCookieParser("_tenantKey")
               .AddEntityFrameworkStore<ApplicationDbContext, TenancyTenant, string>();



            services.AddSession(options =>
            {
                // Set a short timeout for easy testing.
                options.IdleTimeout = TimeSpan.FromMinutes(60);
                // You might want to only set the application cookies over a secure connection:
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.Cookie.HttpOnly = true;
                // Make the session cookie essential
                options.Cookie.IsEssential = true;
            });
            services.AddRazorPages();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            InitializeDbTestData(app);

            app.UseMultiTenancy<TenancyTenant>();

          

            app.UseHttpsRedirection();

            app.UseStaticFiles();

            app.UseRouting();

            app.UseSession();

            app.UseIdentityServer();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
            });
        }

        private static void InitializeDbTestData(IApplicationBuilder app)
        {
            using (var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope())
            {

               //   serviceScope.ServiceProvider.GetRequiredService<PersistedGrantDbContext>().Database.Migrate();
               // serviceScope.ServiceProvider.GetRequiredService<ConfigurationDbContext>().Database.Migrate();
                serviceScope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.Migrate();

                var tenantMgr = serviceScope.ServiceProvider.GetRequiredService<TenantManager<TenancyTenant>>();

                var tenant1 = tenantMgr.FindByCanonicalNameAsync("raj").Result;
                if (tenant1 == null)
                {
                    tenant1 = new TenancyTenant
                    {
                        CanonicalName = "raj",
                        NormalizedCanonicalName = "raj".ToUpperInvariant(),
                    };
                    var result = tenantMgr.CreateAsync(tenant1).Result;
                    if (!result.Succeeded)
                    {
                        throw new Exception(result.Errors.First().Description);
                    }

                    Console.WriteLine("raj created");
                }
                else
                {
                    Console.WriteLine("raj already exists");
                }

                var tenant2 = tenantMgr.FindByCanonicalNameAsync("vishnu").Result;
                if (tenant2 == null)
                {
                    tenant2 = new TenancyTenant
                    {
                        CanonicalName = "vishnu",
                        NormalizedCanonicalName = "vishnu".ToUpperInvariant(),
                    };
                    var result = tenantMgr.CreateAsync(tenant2).Result;
                    if (!result.Succeeded)
                    {
                        throw new Exception(result.Errors.First().Description);
                    }

                    Console.WriteLine("vishnu created");
                }
                else
                {
                    Console.WriteLine("vishnu already exists");
                }

                var tenant = tenantMgr.FindByCanonicalNameAsync("vishnu").Result;
                var tenancyContext = serviceScope.ServiceProvider.GetService<ITenancyContext<TenancyTenant>>();
                tenancyContext.Tenant = tenant;

                var context = serviceScope.ServiceProvider.GetRequiredService<ApplicationDbContext>(); 

                if (!context.Clients.Any())
                {
                    foreach (var client in Clients.Get())
                    {
                        context.Clients.Add(client.ToEntity());
                    }
                    context.SaveChanges();
                }

                if (!context.IdentityResources.Any())
                {
                    foreach (var resource in Resources.GetIdentityResources())
                    {
                        context.IdentityResources.Add(resource.ToEntity());
                    }
                    context.SaveChanges();
                }

                if (!context.ApiScopes.Any())
                {
                    foreach (var scope in Resources.GetApiScopes())
                    {
                        context.ApiScopes.Add(scope.ToEntity());
                    }
                    context.SaveChanges();
                }

                if (!context.ApiResources.Any())
                {
                    foreach (var resource in Resources.GetApiResources())
                    {
                        context.ApiResources.Add(resource.ToEntity());
                    }
                    context.SaveChanges();
                }

                //var userManager = serviceScope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
                //if (!userManager.Users.Any())
                //{
                //    foreach (var testUser in Users.Get())
                //    {
                //        var identityUser = new IdentityUser(testUser.Username)
                //        {
                //            Id = testUser.SubjectId
                //        };

                //        userManager.CreateAsync(identityUser, "Password123!").Wait();
                //        userManager.AddClaimsAsync(identityUser, testUser.Claims.ToList()).Wait();
                //    }
                //}
            }
        }
    }
}
