﻿using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MusicStore.SiteMap.Extensions.DependencyInjection;
using MusicStore.WebHost.Data;
using MusicStore.WebHost.Infrastructure;
using MusicStore.WebHost.Infrastructure.MVC;
using MusicStore.WebHost.Infrastructure.Providers;
using MusicStore.WebHost.Models;
using MusicStore.WebHost.Repositories;
using System;
using System.Globalization;

namespace MusicStore.WebHost
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
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.AddLocalization(opts => opts.ResourcesPath = "Resources");

            services.Configure<RequestLocalizationOptions>(opts =>
            {
                opts.DefaultRequestCulture = new RequestCulture("en-US");
                // Formatting numbers, dates, etc.
                opts.SupportedCultures = new CultureInfo[] { CultureInfo.GetCultureInfo("en-US"), CultureInfo.GetCultureInfo("pt-BR") };
                // UI strings that we have localized.
                opts.SupportedUICultures = new CultureInfo[] { CultureInfo.GetCultureInfo("en-US"), CultureInfo.GetCultureInfo("pt-BR") };
            });

            services.AddMvc(opts =>
            {
                opts.Conventions.Add(new RouteTokenTransformerConvention(new SlugifyParameterTransformer()));
            })
            .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
            .AddDataAnnotationsLocalization()
            .SetCompatibilityVersion(CompatibilityVersion.Version_2_2);

            string connectionString = Configuration.GetConnectionString("Default");
            services.AddDbContextPool<MusicStoreDbContext>(opts => opts.UseSqlServer(connectionString));

            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<MusicStoreDbContext>()
                .AddDefaultUI(Microsoft.AspNetCore.Identity.UI.UIFramework.Bootstrap4)
                .AddDefaultTokenProviders();

            services.AddAuthorization(opts =>
            {
                opts.AddPolicy(PolicyNames.ADMIN, policy => policy.RequireRole(RoleNames.ADMINISTRATOR));
            });

            services.AddSession(opts =>
            {
                opts.Cookie.Name = Guid.NewGuid().ToString();
                opts.Cookie.HttpOnly = true;
                opts.Cookie.IsEssential = true;
                opts.IdleTimeout = TimeSpan.FromMinutes(3);
            });

            services.AddOptions();

            services.AddHttpContextAccessor();
            services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();

            services.AddTransient<ISessionProvider, HttpSessionProvider>();
            services.AddTransient<IClaimsPrincipalProvider, HttpClaimsPrincipalProvider>();

            services.AddTransient<IAlbumRepository, EFAlbumRepository>();
            services.AddTransient<IGenreRepository, EFGenreRepository>();
            services.AddTransient<IOrderRepository, EFOrderRepository>();

            services.AddScoped<ShoppingCart>();

            services.AddScoped<IUrlHelper>(x =>
            {
                var actionContext = x.GetRequiredService<IActionContextAccessor>().ActionContext;
                var factory = x.GetRequiredService<IUrlHelperFactory>();
                return factory.GetUrlHelper(actionContext);
            });

            services.AddFileSiteMapProvider(opts =>
            {
                opts.File = "wwwroot/mvc.sitemap";
                opts.ResourceName = "SiteMap";
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();

            var locOptions = app.ApplicationServices.GetService<IOptions<RequestLocalizationOptions>>();
            app.UseRequestLocalization(locOptions.Value);

            app.UseAuthentication();

            app.UseSession();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "areas",
                    template: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
