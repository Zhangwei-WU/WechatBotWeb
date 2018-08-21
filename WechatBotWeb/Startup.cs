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
           
            services.AddSingleton<IAuthenticationService>((p) =>
            {
                var svc = new AuthenticationService(
                    p.GetService<IApplicationInsights>(),
                    keyVaultClient,
                    "https://wechatybot-test-keyvault.vault.azure.net/secrets/storagetableconnstring-1/03fd03bab8634048b6a10642aad8727c",
                    "https://wechatybot-test-keyvault.vault.azure.net/keys/authentication-key-1/ccb5384bc2124f89822e53a5e1800bed",
                    "https://wechatybot-test-keyvault.vault.azure.net/secrets/authentication-aes-1/94ff9cd12e1142099e8833ebf23bc1df");
                Task.Run(async () => await svc.InitializeAsync()).Wait();
                return svc;
            });
            
            services.AddSingleton<IAppAuthenticationService>((p)=>p.GetService<IAuthenticationService>() as AuthenticationService);
            services.AddSingleton<IUserAuthenticationService>((p) => p.GetService<IAuthenticationService>() as AuthenticationService);

            services.AddSingleton<IClientManagementService, ClientManagementService>((p) =>
            {
                var svc = new ClientManagementService(
                    p.GetService<IApplicationInsights>(),
                    keyVaultClient,
                    "https://wechatybot-test-keyvault.vault.azure.net/secrets/storagetableconnstring-1/03fd03bab8634048b6a10642aad8727c");
                Task.Run(async () => await svc.InitializeAsync()).Wait();
                return svc;
            });

            services.AddSingleton<ExceptionHandlingOptions, ExceptionHandlingOptions>();
            
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
            }
            else
            {
                app.UseHsts();
            }

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
