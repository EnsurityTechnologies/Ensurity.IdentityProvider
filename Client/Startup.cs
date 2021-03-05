using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Logging;

namespace Client
{
    public class Startup
    {

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();
            IdentityModelEventSource.ShowPII = true;
            services.AddAuthentication(options =>
            {
                options.DefaultScheme = "cookie";
                options.DefaultChallengeScheme = "oidc";


            })
                .AddCookie("cookie")
                .AddOpenIdConnect("oidc", options =>
                {

                    options.Authority = "https://raj.ensurity.com";
                    options.ClientId = "oidcClient";
                    options.ClientSecret = "SuperSecretPassword";

                    options.ResponseType = "code";
                    options.UsePkce = true;

                    options.NonceCookie.SameSite = SameSiteMode.Unspecified;
                    options.CorrelationCookie.SameSite = SameSiteMode.Unspecified;

                    options.ResponseMode = "query";

                   // options.CallbackPath = "/signin-oidc"; // default redirect URI

                    //options.Scope.Add("oidc"); // default scope
                   // options.Scope.Add("profile"); // default scope
                    options.Scope.Add("api1.read");
                   // options.Scope.Add("email");
                   // options.Scope.Add("role");
                    options.SaveTokens = true;
                    
                  

                });

           
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app)
        {

          

            app.UseDeveloperExceptionPage();
            app.UseHttpsRedirection();

            app.UseStaticFiles();
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints => endpoints.MapDefaultControllerRoute());
        }
    }
}
