using Microsoft.Extensions.DependencyInjection;
using System;
using Microsoft.AspNetCore.Mvc;

namespace CodeVault.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApiServices(this IServiceCollection services)
        {
            // Configure API behavior options
            services.Configure<ApiBehaviorOptions>(options =>
            {
                options.SuppressModelStateInvalidFilter = true;
            });

            // Add CORS policy for the API
            services.AddCors(options =>
            {
                options.AddPolicy("ApiCorsPolicy",
                    builder => builder
                        .AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader());
            });

            return services;
        }
    }
}