/*
 * Copyright (c) 2018 Sveriges Radio AB, Stockholm, Sweden
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions
 * are met:
 * 1. Redistributions of source code must retain the above copyright
 *    notice, this list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright
 *    notice, this list of conditions and the following disclaimer in the
 *    documentation and/or other materials provided with the distribution.
 * 3. The name of the author may not be used to endorse or promote products
 *    derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE AUTHOR ``AS IS'' AND ANY EXPRESS OR
 * IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.
 * IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY DIRECT, INDIRECT,
 * INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
 * NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
 * DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
 * THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF
 * THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using CCM.Core.Helpers;
using CCM.DiscoveryApi.Authentication;
using CCM.DiscoveryApi.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Serialization;

namespace CCM.DiscoveryApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        /// <summary>
        /// This method gets called by the runtime. Use this method to add services to the container.
        /// </summary>
        /// <param name="services"></param>
        public void ConfigureServices(IServiceCollection services)
        {
            // Application deploy information
            services.Configure<ApplicationBuildInformation>(Configuration.GetSection("Build"));

            // Add Cross Origin support
            services.AddCors();

            // Authentication and authorization (Basic)
            // The registered authentication handlers and their configuration options are called "schemes".
            // An authentication scheme is named when the authentication service is configured during authentication.
            services.AddAuthentication("BasicAuthenticationDiscoveryV2") // Default
                .AddScheme<AuthenticationSchemeOptions, DiscoveryV2BasicAuthenticationHandler>("BasicAuthenticationDiscoveryV2", null)
                .AddScheme<AuthenticationSchemeOptions, DiscoveryV1BasicAuthenticationHandler>("BasicAuthenticationDiscoveryV1", null);

            services.AddAuthorization(opt =>
            {
                opt.AddPolicy("BasicAuthenticationDiscoveryV2", policy =>
                {
                    policy.AddAuthenticationSchemes("BasicAuthenticationDiscoveryV2");
                    policy.RequireAuthenticatedUser();
                });
                opt.AddPolicy("BasicAuthenticationDiscoveryV1", policy =>
                {
                    policy.AddAuthenticationSchemes("BasicAuthenticationDiscoveryV1");
                    policy.RequireAuthenticatedUser();
                });
            });

            services.AddControllers(options =>
                {
                    options.RespectBrowserAcceptHeader = true; // false by default

                    //var xmlWriterSettings = options.OutputFormatters
                    //    .OfType<XmlSerializerOutputFormatter>()
                    //    .Single()
                    //    .WriterSettings;

                    //xmlWriterSettings.Indent = false;
                })
                .AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                })
                .AddXmlSerializerFormatters()
                .AddXmlDataContractSerializerFormatters();

            // Used to forward requests to the Discovery API's
            services.AddHttpClient();

            // Register dependency injection
            services.AddScoped<IDiscoveryHttpService, DiscoveryHttpService>();
        }

        /// <summary>
        /// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        /// </summary>
        /// <param name="app"></param>
        /// <param name="env"></param>
        /// <param name="logger"></param>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger)
        {
            logger.LogInformation($"#################### CCM Discovery starting up...");

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            //app.UseHttpsRedirection();

            // Set global CORS policy
            app.UseCors(x => x
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowAnyOrigin()
                .AllowCredentials());

            // Who are you? (Basic authentication)
            app.UseAuthentication();

            // Find out routing
            app.UseRouting();

            //Are you allowed?
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();

                // Default route
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");

                // Api routes
                endpoints.MapControllerRoute(
                    name: "DiscoveryApi",
                    pattern: "api/{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
