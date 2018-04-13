using System;
using Aggregator.Autofac;
using Aggregator.Example.Domain;
using Aggregator.Example.WebHost.Projections;
using Aggregator.Example.WebHost.Projections.Infrastructure;
using Aggregator.Persistence;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SpaServices.Webpack;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Aggregator.Example.WebHost
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;

            AppDomain.CurrentDomain.Load(typeof(Dummy).Assembly.FullName);
        }

        public IContainer ApplicationContainer { get; private set; }

        public IConfiguration Configuration { get; }

        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();

            var builder = new ContainerBuilder();
            builder.Populate(services);
            builder.RegisterModule<AggregatorModule>();
            builder
                .RegisterType<Persistence.EventStore.EventStore>()
                .WithParameter("connectionString", Configuration.GetConnectionString("EventStore"))
                .As<IEventStore<string, object>>()
                .SingleInstance();

            builder
                .RegisterType<UserProjection>()
                .AsImplementedInterfaces()
                .SingleInstance();

            builder
                .RegisterType<EventStoreProjector>()
                .WithParameter("connectionString", Configuration.GetConnectionString("EventStore"))
                .SingleInstance();

            return new AutofacServiceProvider(ApplicationContainer = builder.Build());
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IApplicationLifetime appLifetime)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseWebpackDevMiddleware(new WebpackDevMiddlewareOptions
                {
                    HotModuleReplacement = true
                });
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });

            app.MapWhen(x => !x.Request.Path.Value.StartsWith("/api"), builder =>
            {
                builder.UseMvc(routes =>
                {
                    routes.MapSpaFallbackRoute(
                        name: "spa-fallback",
                        defaults: new { controller = "Home", action = "Index" });
                });
            });

            app.ApplicationServices.GetService<EventStoreProjector>().Start().Wait();

            appLifetime.ApplicationStopped.Register(() =>
            {
                ApplicationContainer.Dispose();
            });
        }
    }
}
