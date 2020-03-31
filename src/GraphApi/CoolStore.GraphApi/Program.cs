using System;
using System.Threading.Tasks;
using HotChocolate;
using HotChocolate.AspNetCore;
using HotChocolate.AspNetCore.Playground;
using HotChocolate.AspNetCore.Subscriptions;
using HotChocolate.Execution;
using HotChocolate.Stitching;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using N8T.Infrastructure.Dapr;
using Serilog;

namespace CoolStore.GraphApi
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateLogger();

            var builder = WebApplication.CreateBuilder(args);

            var configurationBuilder = builder.Configuration
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);

            var env = builder.Environment;
            configurationBuilder.AddJsonFile("services.json", optional: true);
            if (env.IsDevelopment())
            {
                var servicesJson = System.IO.Path.Combine(env.ContentRootPath, "..", "..", "..", "services.json");
                configurationBuilder.AddJsonFile(servicesJson, optional: true);
            }

            var config = configurationBuilder.Build();

            builder.Host
                .UseSerilog();

            builder.Services.AddHttpContextAccessor();

            builder.Services.AddHttpClient("product_catalog",
                (sp, client) =>
                {
                    client.BaseAddress = new Uri($"{config.GetDaprClientUrl("product-catalog-api")}/graphql");
                });

            // builder.Services.AddHttpClient("inventory",
            //     (sp, client) =>
            //     {
            //         client.BaseAddress = new Uri($"{serviceOptions.InventoryService.RestUri}/graphql");
            //     });
            // builder.Services.AddHttpClient("shopping_cart",
            //     (sp, client) =>
            //     {
            //         client.BaseAddress = new Uri($"{serviceOptions.ShoppingCartService.RestUri}/graphql");
            //     });

            builder.Services.AddSingleton<IQueryResultSerializer, JsonQueryResultSerializer>();
            builder.Services
                .AddGraphQLSubscriptions()
                .AddStitchedSchema(stitchingBuilder => stitchingBuilder
                    .AddSchemaFromHttp("product_catalog")
                    //.AddSchemaFromHttp("inventory")
                    //.AddSchemaFromHttp("shopping_cart")
                );

            var app = builder.Build();

            app.UseStaticFiles();
            app.UseGraphQL("/graphql");
            app.UsePlayground(new PlaygroundOptions
            {
                QueryPath = "/graphql",
                Path = "/playground",
            });

            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", context =>
                {
                    context.Response.Redirect("/playground");
                    return Task.CompletedTask;
                });
            });

            await app.RunAsync();
        }
    }
}
