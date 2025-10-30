
using Biedapp.API.Extensions;
using Biedapp.Application.Interfaces;
using Biedapp.Application.Services;
using Biedapp.Infrastructure.Configuration;
using Biedapp.Infrastructure.EventStore;
using Biedapp.Infrastructure.Security;

using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

namespace Biedapp.API;

public class Program
{
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        IConfiguration configuration = builder.Configuration;

        builder.AddServiceDefaults();

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Biedapp Core API",
                Version = "v1",
                Description = "Home Budget Management API with Event Sourcing and CQS",
                Contact = new OpenApiContact
                {
                    Name = "Biedapp",
                    Email = "support@biedapp.local"
                }
            });
        });

        builder.Services.AddCorsSupport();
        builder.Services.AddEventStoreSupport(configuration);

        builder.Services.AddScoped<IBudgetService, BudgetService>();
        builder.Services.AddScoped<IStatisticsService, StatisticsService>();

        WebApplication app = builder.Build();

        app.MapDefaultEndpoints();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Biedapp API V1");
                options.RoutePrefix = "swagger";
                options.DocumentTitle = "Biedapp API Documentation";
            });
        }

        app.UseCors(ApiConstants.FrontendClientCorsPolicyName);

        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();

        ILogger<Program> logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("BiedApp API started successfully");
        logger.LogInformation("Swagger UI available at: https://localhost:{Port}/swagger",
            app.Urls.FirstOrDefault()?.Split(':').LastOrDefault() ?? "5001");

        app.Run();
    }
}
