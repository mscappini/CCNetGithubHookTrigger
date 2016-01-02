using Microsoft.Owin.Hosting;
using Owin;
using SimpleInjector;
using SimpleInjector.Integration.WebApi;
using System;
using System.Web.Http;

namespace CruiseControl.Core.Triggers
{
    internal class OwinStartup
    {
        public static IDisposable Start(GithubHookTrigger trigger)
        {
            StartOptions options = new StartOptions(trigger.EndPoint);
            return WebApp.Start(options, (app) => Configuration(app, trigger));
        }

        private static void Configuration(IAppBuilder app, GithubHookTrigger trigger)
        {
            trigger.Log("Configuring app");

            HttpConfiguration config = new HttpConfiguration();

            // CORS
            //config.EnableCors(new EnableCorsAttribute("*", "*", "POST"));

            // Routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "{*post}",
                defaults: new { controller = "Default", action = "post" }
            );

            // Dependency Injection to inject trigger into controller ctors.
            trigger.Log("Configuring dependency injection IoC");
            Container container = new Container();
            container.RegisterSingleton<GithubHookTrigger>(trigger);
            container.RegisterSingleton<BodyDigestMiddleware>(() => new BodyDigestMiddleware(trigger));
            container.RegisterWebApiControllers(config);
            config.DependencyResolver = new SimpleInjectorWebApiDependencyResolver(container);

            // Body reader middleware.
            app.Use(async (context, next) =>
            {
                BodyDigestMiddleware bodyDigest = container.GetInstance<BodyDigestMiddleware>();
                await bodyDigest.Invoke(context, next);
            });

            trigger.Log("Configuring app to use Web API");
            app.UseWebApi(config);

            trigger.Log("Configured app");
        }
    }
}