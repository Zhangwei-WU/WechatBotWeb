namespace WechatBotWeb
{
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.HttpsPolicy;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.SpaServices.AngularCli;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using WechatBotWeb.Common;
    using WechatBotWeb.Insight;
    using WechatBotWeb.Middlewares;
    using WechatBotWeb.IData;
    using WechatBotWeb.TableData;
    using Microsoft.WindowsAzure.Storage.Table;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.Azure.KeyVault;
    using Microsoft.Azure.Services.AppAuthentication;
    using System.Threading.Tasks;

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
            services.AddSingleton<IApplicationInsights, AzureApplicationInsights>();

            AzureServiceTokenProvider azureServiceTokenProvider = new AzureServiceTokenProvider();
            var keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));
           
            services.AddSingleton<IAuthenticationService>((p) => new AuthenticationService(p.GetService<IApplicationInsights>(), keyVaultClient, "StorageTableConnString-0", "SignKey-0", "EncryptionKey-0"));

            services.AddSingleton<IAppAuthenticationService>((p)=>p.GetService<IAuthenticationService>() as AuthenticationService);
            services.AddSingleton<IUserAuthenticationService>((p) => p.GetService<IAuthenticationService>() as AuthenticationService);

            services.AddSingleton<IClientManagementService, ClientManagementService>((p) => new ClientManagementService(p.GetService<IApplicationInsights>(), keyVaultClient, "StorageTableConnString-0"));
            services.AddScoped<ExceptionHandlingOptions, ExceptionHandlingOptions>();

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            // In production, the Angular files will be served from this directory
            services.AddSpaStaticFiles(configuration =>
            {
                configuration.RootPath = "ClientApp/dist";
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            GlobalVariables.IsProduction = env.IsProduction();

            if (env.IsDevelopment())
            {
                //app.UseDeveloperExceptionPage();
            }
            else
            {
                //app.UseExceptionHandler("/Error");
                app.UseHsts();
            }


            //app.Map(GlobalVariables.WebApiPath, (ap) =>
            //{
            //    ap.UseMvc(routes =>
            //    {
            //        routes.MapRoute(
            //            name: "default",
            //            template: "{controller}/{action=Index}/{id?}");
            //    });
            //});

            app.UseWhen((context) => context.Request.Path.StartsWithSegments(GlobalVariables.WebApiPath), (ap) =>
              {
                  ap.UseClientInfoMiddleware();
                  ap.UseExceptionHandlerMiddleware();
                  ap.UseJwtAuthenticationMiddleware();
              });
            
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseSpaStaticFiles();
            
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller}/{action=Index}/{id?}");
            });

            app.UseSpa(spa =>
            {
                // To learn more about options for serving an Angular SPA from ASP.NET Core,
                // see https://go.microsoft.com/fwlink/?linkid=864501

                spa.Options.SourcePath = "ClientApp";

                if (env.IsDevelopment())
                {
                    spa.UseAngularCliServer(npmScript: "start");
                }
            });
        }
    }
}
