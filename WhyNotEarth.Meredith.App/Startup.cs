﻿using System;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RollbarDotNet.Core;
using RollbarDotNet.Logger;
using WhyNotEarth.Meredith.App.Auth;
using WhyNotEarth.Meredith.App.Configuration;
using WhyNotEarth.Meredith.App.Localization;
using WhyNotEarth.Meredith.App.Middleware;
using WhyNotEarth.Meredith.App.Swagger;
using WhyNotEarth.Meredith.Data.Entity;
using WhyNotEarth.Meredith.DependencyInjection;
using WhyNotEarth.Meredith.Volkswagen;

[assembly: ApiController]
namespace WhyNotEarth.Meredith.App
{
    public class Startup
    {
        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors(o => o
                .AddDefaultPolicy(builder => builder
                    .SetIsOriginAllowed(origin => true)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials()));

            services.AddRollbarWeb();

            services.AddCustomOptions(_configuration);

            services.AddDbContext<MeredithDbContext>(o => o.UseNpgsql(_configuration.GetConnectionString("Default"),
                options => options.SetPostgresVersion(new Version(9, 6))));

            services.AddMeredith();

            services.AddTransient(s => s.GetService<IHttpContextAccessor>().HttpContext.User)
                .Configure<ForwardedHeadersOptions>(options =>
                {
                    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                    options.ForwardLimit = null;
                    options.RequireHeaderSymmetry = false;
                    options.KnownNetworks.Clear();
                    options.KnownProxies.Clear();
                });

            services.AddSwagger();

            services.AddHangfire(c => c.SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(_configuration.GetConnectionString("Default")));

            services.AddHangfireServer();

            services.AddCustomAuthentication(_configuration);

            services.AddCustomAuthorization();

            services.AddControllers()
                .AddMvcOptions(options =>
                {
                    // https://github.com/dotnet/aspnetcore/issues/11584
                    options.ModelBinderProviders.Insert(0, new DateTimeModelBinderProvider());
                }).AddNewtonsoftJson();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory,
            IRecurringJobManager recurringJobManager, IServiceProvider serviceProvider)
        {
            app.UseForwardedHeaders();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                loggerFactory.AddRollbarDotNetLogger(app.ApplicationServices);
            }

            using (var serviceScope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                using var context = serviceScope.ServiceProvider.GetService<MeredithDbContext>();
                context.Database.Migrate();
            }

            app.UseCustomLocalization();

            app.UseCustomSwagger();

            app.UseMiddleware<ExceptionHandlingMiddleware>();

            app.UseStaticFiles();

            app.UseRouting();

            app.UseCors();

            app.UseCustomAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();

                endpoints.MapHangfireDashboard(new DashboardOptions
                    {
                        Authorization = new IDashboardAuthorizationFilter[] { }
                    })
                    .RequireAuthorization(Policies.Developer);
            });

            app.UseHangfireDashboard();

            // Every 15 minutes
            recurringJobManager.AddOrUpdate("JumpStartService_SendAsync",
                () => serviceProvider.GetService<JumpStartService>().SendAsync(),
                "*/15 * * * *", TimeZoneInfo.Utc);
        }
    }
}