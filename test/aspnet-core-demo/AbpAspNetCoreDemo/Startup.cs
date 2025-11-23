using System;
using System.Text.Json;
using Abp.AspNetCore;
using Abp.AspNetCore.Configuration;
using Abp.AspNetCore.Mvc.Antiforgery;
using Abp.AspNetCore.Mvc.Extensions;
using Abp.HtmlSanitizer;
using Abp.Json.SystemTextJson;
using AbpAspNetCoreDemo.Controllers;
using Autofac;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AbpAspNetCoreDemo;

/// <summary>
/// Startup class using modern ASP.NET Core + Autofac pattern
/// </summary>
public class Startup {
    public Startup(IConfiguration configuration) {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services) {
        services.AddSingleton(Configuration);

        // Test classes
        services.AddTransient<MyTransientClass1>();
        services.AddTransient<MyTransientClass2>();
        services.AddScoped<MyScopedClass>();

        // Add framework services
        services.AddMvc(options => { options.Filters.Add(new AbpAutoValidateAntiforgeryTokenAttribute()); }).AddRazorRuntimeCompilation();

        services.AddOptions<JsonOptions>()
            .Configure<IServiceProvider>((options, rootServiceProvider) => {
                options.JsonSerializerOptions.ReadCommentHandling = JsonCommentHandling.Skip;
                options.JsonSerializerOptions.AllowTrailingCommas = true;

                options.JsonSerializerOptions.Converters.Add(new AbpStringToEnumFactory());
                options.JsonSerializerOptions.Converters.Add(new AbpStringToBooleanConverter());
                options.JsonSerializerOptions.Converters.Add(new AbpStringToGuidConverter());
                options.JsonSerializerOptions.Converters.Add(new AbpNullableStringToGuidConverter());
                options.JsonSerializerOptions.Converters.Add(new AbpNullableFromEmptyStringConverterFactory());
                options.JsonSerializerOptions.Converters.Add(new ObjectToInferredTypesConverter());
                options.JsonSerializerOptions.Converters.Add(new DateOnlyJsonConverter());
                options.JsonSerializerOptions.Converters.Add(new CultureInvariantDecimalJsonConverter());
                options.JsonSerializerOptions.Converters.Add(new CultureInvariantDoubleJsonConverter());

                var aspNetCoreConfiguration = rootServiceProvider.GetRequiredService<IAbpAspNetCoreConfiguration>();
                options.JsonSerializerOptions.TypeInfoResolver =
                    new AbpDateTimeJsonTypeInfoResolver(aspNetCoreConfiguration.InputDateTimeFormats, aspNetCoreConfiguration.OutputDateTimeFormat);
            });

        services.Configure<MvcOptions>(x => x.AddAbpHtmlSanitizer());

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            // options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "AbpAspNetCoreDemo API", Version = "v1" });
            options.DocInclusionPredicate((docName, description) => true);
        });
    }

    // ConfigureContainer is called AFTER ConfigureServices
    // Use this to register things directly with Autofac
    public void ConfigureContainer(ContainerBuilder builder) {
        // The builder already has ABP services registered from AddAbpWithoutBuildingContainer
        // But we need to populate any services that were added in ConfigureServices above
        // This is handled by the framework - no action needed here
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
        // Add exception logging middleware
        app.Use(async (context, next) => {
            try {
                await next();
            } catch (Exception ex) {
                Console.WriteLine($"=== EXCEPTION CAUGHT ===");
                Console.WriteLine($"Message: {ex.Message}");
                Console.WriteLine($"Type: {ex.GetType().FullName}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                if (ex.InnerException != null) {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                    Console.WriteLine($"Inner StackTrace: {ex.InnerException.StackTrace}");
                }
                Console.WriteLine($"======================");
                throw;
            }
        });
        
        app.UseAbp(options => { 
            options.UseAbpRequestLocalization = false;
        }); // Initialize ABP framework - must be first

        app.UseSwagger();
        app.UseSwaggerUI();

        // Return IQueryable from controllers
        app.UseUnitOfWork(options => { options.Filter = httpContext => httpContext.Request.Path.Value.StartsWith("/odata"); });

        // Always use developer exception page to see detailed errors
        app.UseDeveloperExceptionPage();
        
        if (env.IsDevelopment()) {
            // Additional dev settings
        }
        else {
            // app.UseExceptionHandler("/Home/Error");
        }

        app.UseStaticFiles();
        app.UseEmbeddedFiles(); // Expose embedded files to the web

        app.UseRouting();
        app.UseAbpHtmlSanitizer(); // Sanitize HTML inputs

        app.UseEndpoints(endpoints => {
            endpoints.MapControllerRoute("defaultWithArea", "{area}/{controller=Home}/{action=Index}/{id?}");
            endpoints.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
            endpoints.MapRazorPages();

            app.ApplicationServices.GetRequiredService<IAbpAspNetCoreConfiguration>()
                .EndpointConfiguration.ConfigureAllEndpoints(endpoints);
        });


    }
}