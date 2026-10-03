using FiapSub.Sales.Api.Infrastructure;
using FiapSub.Sales.Api.Time;
using FiapSub.Sales.Application;
using FiapSub.Sales.Application.Abstractions;
using FiapSub.Sales.Infrastructure.Database;
using FiapSub.Sales.Infrastructure.Http;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(swagger => swagger.SwaggerDoc("v1", new OpenApiInfo
{
    Title = "FiapSub Sales — listings and purchases",
    Version = "v1",
    Description =
        "Serves the price-ordered public listings and executes purchases, from its own database. "
        + "The vehicle catalogue and the payment webhook live in the core service.",
}));

// The MVC model binder already rejects malformed bodies; let that answer be a ProblemDetails too.
builder.Services.Configure<ApiBehaviorOptions>(options => options.SuppressMapClientErrors = false);

builder.Services.Configure<InternalApiOptions>(
    builder.Configuration.GetSection(InternalApiOptions.SectionName));

builder.Services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
builder.Services.AddSingleton<IPaymentCodeGenerator, PaymentCodeGenerator>();
builder.Services.AddApplication();
builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddServiceGateways(builder.Configuration);

builder.Services.AddExceptionHandler<DomainExceptionHandler>();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<SalesDbContext>("sales-database", tags: ["ready"]);

var app = builder.Build();

await app.MigrateAsync();

app.UseExceptionHandler();

app.UseSwagger();
app.UseSwaggerUI(ui => ui.SwaggerEndpoint("/swagger/v1/swagger.json", "FiapSub Sales v1"));

app.UseMiddleware<InternalApiTokenMiddleware>();

app.MapControllers();

// Liveness answers as soon as the process is up; readiness also needs the database.
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });

await app.RunAsync();

/// <summary>Exposed so the integration tests can boot the real pipeline with WebApplicationFactory.</summary>
public partial class Program;
