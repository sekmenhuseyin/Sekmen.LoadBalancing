using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Infrastructure.DependencyInjection;
using Umbraco.Cms.Web.Website.Controllers;
using UmbracoLoadBalancingTraining.Web.Controllers;
using UmbracoLoadBalancingTraining.Web.NotificationHandlers;

namespace UmbracoLoadBalancingTraining.Web
{
    public class Startup
    {
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;

        /// <summary>
        /// Initializes a new instance of the <see cref="Startup" /> class.
        /// </summary>
        /// <param name="webHostEnvironment">The web hosting environment.</param>
        /// <param name="config">The configuration.</param>
        /// <remarks>
        /// Only a few services are possible to be injected here https://github.com/dotnet/aspnetcore/issues/9337
        /// </remarks>
        public Startup(IWebHostEnvironment webHostEnvironment, IConfiguration config)
        {
            _env = webHostEnvironment ?? throw new ArgumentNullException(nameof(webHostEnvironment));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// Configures the services.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <remarks>
        /// This method gets called by the runtime. Use this method to add services to the container.
        /// For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        /// </remarks>
        public void ConfigureServices(IServiceCollection services)
        {
            //Adds Microsoft SQL Server distributed caching services to the specified IServiceCollection 
            services.AddDistributedSqlServerCache(options =>
            {
                options.ConnectionString = _config.GetConnectionString(
                    "umbracoDbDSN");
                options.SchemaName = "dbo";
                options.TableName = "DistCache";
            });

            //Registers the output caching service with the dependency injection system
            services.AddOutputCaching();
            services.Configure<UmbracoRenderingDefaultsOptions>(c =>
            {
                c.DefaultControllerType = typeof(DefaultController);
            });

#pragma warning disable IDE0022 // Use expression body for methods
            services.AddUmbraco(_env, _config)
                .AddBackOffice()
                .AddWebsite()
                .AddComposers()
                .AddNotificationHandler<ContentCacheRefresherNotification, OutputCacheContentCacheHandler>()
                .AddNotificationHandler<MediaCacheRefresherNotification, OutputCacheContentCacheHandler>()
                .AddNotificationHandler<DictionaryCacheRefresherNotification, OutputCacheContentCacheHandler>()
                .SetServerRegistrar<LoadBalancingTrainingServerRoleAccessor>()
                .Build();
#pragma warning restore IDE0022 // Use expression body for methods

        }

        /// <summary>
        /// Configures the application.
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <param name="env">The web hosting environment.</param>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            //Registers the output caching middleware
            app.UseOutputCaching();

            app.UseUmbraco()
                .WithMiddleware(u =>
                {
                    u.UseBackOffice();
                    u.UseWebsite();
                })
                .WithEndpoints(u =>
                {
                    u.UseInstallerEndpoints();
                    u.UseBackOfficeEndpoints();
                    u.UseWebsiteEndpoints();
                });
        }
    }
}
