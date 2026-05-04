using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;
using Y.Threads.Infrastructure.Middlewares;

var cultureInfo = CultureInfo.CreateSpecificCulture("en-US");
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

Log.Logger = new LoggerConfiguration()
  .Enrich.FromLogContext()
  .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

try
{
    builder.Services.AddSerilog((services, loggerConfiguration) => loggerConfiguration
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    ThreadsDependencyInjection();

    var profilesPresentationAssembly = typeof(Y.Profiles.Presentation.AssemblyReference).Assembly;
    var articlesPresentationAssembly = typeof(Y.Articles.Presentation.AssemblyReference).Assembly;
    var threadsPresentationAssembly = typeof(Y.Threads.Presentation.AssemblyReference).Assembly;

    builder.Services
        .AddControllers()
        .AddApplicationPart(profilesPresentationAssembly)
        .AddApplicationPart(articlesPresentationAssembly)
        .AddApplicationPart(threadsPresentationAssembly);

    var authSecret = Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]!);

    builder.Services
        .AddAuthorization()
        .AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(authSecret),
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidAudience = builder.Configuration["Jwt:Audience"],
                ClockSkew = TimeSpan.Zero
            };
        });

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddOpenApi();

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference();
    }

    app.UseMiddleware<LoggingCorrelationMiddleware>();

    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "An unhandled exception occurred during bootstrapping");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

void ThreadsDependencyInjection()
{
    Y.Threads.Domain.DependencyInjection.AddDomain(builder.Services, builder.Configuration);
    Y.Threads.Infrastructure.DependencyInjection.AddInfrastructure(builder.Services, builder.Configuration);
    Y.Threads.Application.DependencyInjection.AddApplication(builder.Services, builder.Configuration);
}
