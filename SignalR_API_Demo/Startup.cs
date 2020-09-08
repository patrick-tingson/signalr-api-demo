using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SignalR_API_Demo.Data;
using SignalR_API_Demo.Extention;
using SignalR_API_Demo.Hubs;

namespace SignalR_API_Demo
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
            string wrongPublicKey = "-----BEGIN PUBLIC KEY-----MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA06pYm1ahzazvS17zSZtQnqeE/fjJBcDNqVeD/kMqctBExND22PMoU9v1kmEvunhShIHPA3blgpLoOaQQD2+BiCwMZjCbAMEIwEl//sYvnICDUv4+UCMn6obwyhGEAldOwMxeVdocDdnAsvIaYflmSaec/ZP11EjZ+zujgimoO+7DxjZ652hTCPd9Mc7Z0i+lCM5MLK1PpNfYmUcwgI9yrOMQapCKKrURM/6XwEMP5gtLN7IXRUkZvI3zrCpD95Dr//x7s/jinylEWLoo7WKk6/eq9eXQOnCS47OMt/Mey4x3nSbZsCTvL2q3/xyselFZyRlfoc8eIqdd6cv6cQe0aQIDAQAB-----END PUBLIC KEY-----";
            string publicKey = "-----BEGIN PUBLIC KEY-----MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAoT2r2y1s/BmiOSzW4mhax90NrPZY16D83ax74BxQS1r37Lw20ozK3ZoCWSnJ1vT0Fwd1wFRJ05xZku+dRPkYkWh9Kx+5+QAh7XCZM8e+8DXtxOomx7DZsBPrjw+MU0FpQltkz9Z/2YA3CDR3HQmc0F1YmTs7CQSNxD5vW1gyGgc4y306XKiWKT0B2rCxCNoZmNH2H/Y+5XlHTRVdn3yKTfJM2ga5fCQRbMxb+gP+aANF8S6SyDN1S3gW1ZtY9rXNkXmBZqWHFPJ2LmVQk+S74w+xUjpvAkPgx1o7hkQkf06wLlQRISZ1gbxcsfxYZyKTVVSHn6pPObT25aytqVLmpQIDAQAB-----END PUBLIC KEY-----";
            string validIssuer = "https://www.testing.com";

            var authPolicy = new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
                    .RequireAuthenticatedUser()
                    .Build();

            services.AddControllers();

            services.AddAuthorization(options =>
            {
                //options.AddPolicy("NotificationSubscriber", policy =>
                //{
                //    policy.Requirements.Add(new NotificationSubscriberRequirement());
                //});
            });

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
              .AddJwtBearer(options =>
              {
                   //options.Authority = "https://localhost:44391/validate";
                   options.TokenValidationParameters = new TokenValidationParameters
                  {
                      IssuerSigningKey = RSASecurityKey(publicKey),
                      RequireExpirationTime = true,
                      ValidateAudience = false,
                      ValidateIssuer = true,
                      ValidateIssuerSigningKey = true,
                      ValidateLifetime = true,
                      ValidIssuer = validIssuer,
                      ClockSkew = TimeSpan.Zero
                  };

                  //Few cases where browser APIs restrict the ability to apply headers (specifically, in Server-Sent Events and WebSockets requests). 
                  //In these cases, the access token is provided as a query string value access_token.
                  options.Events = new JwtBearerEvents
                  {
                      OnMessageReceived = context =>
                      {
                          var accessToken = context.Request.Query["access_token"];

                          // If the request is for our hub...
                          var path = context.HttpContext.Request.Path;
                          if (!string.IsNullOrEmpty(accessToken) &&
                              (path.StartsWithSegments("/notificationhub")))
                          {
                              // Read the token out of the query string
                              context.Token = accessToken;
                          }
                          return Task.CompletedTask;
                      }
                  };
              });


            services.AddMvc(options =>
            {
                options.Filters.Add(new AuthorizeFilter(authPolicy));
            });

            services.AddCors(options => options.AddPolicy("CorsPolicy",
                builder =>
                {
                    builder.AllowAnyMethod().AllowAnyHeader()
                           .WithOrigins("https://localhost:44301", 
                                "http://localhost:64648",
                                "http://localhost:5000",
                                "https://localhost:5001",
                                "http://localhost:3000", 
                                "http://localhost:80", 
                                "http://localhost:8080")
                           .AllowCredentials();
                }));

            services.AddSignalR(hubOptions =>
            {
                hubOptions.EnableDetailedErrors = true;
                hubOptions.KeepAliveInterval = TimeSpan.FromMinutes(1);
            });

            services.AddSingleton<IUserIdProvider, GlobalIdBasedUserIdProvider>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseRouting();

            app.UseCors("CorsPolicy");

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHub<StronglyTypeNotificationHub>("/notificationhub", option =>
                {
                    option.LongPolling.PollTimeout = new TimeSpan(0, 5, 0);
                });
            });
        }

        private RsaSecurityKey RSASecurityKey(string publicKey)
        {
            var rsa = new System.Security.Cryptography.RSACryptoServiceProvider();
            rsa.LoadPublicKeyPEM(publicKey);
            return new RsaSecurityKey(rsa);
        }
    }
}
