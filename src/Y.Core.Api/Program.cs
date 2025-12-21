using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;

var cultureInfo = CultureInfo.CreateSpecificCulture("en-US");
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

Log.Logger = new LoggerConfiguration()
  .Enrich.FromLogContext()
  .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

try
{
    ThreadsDependencyInjection();

    var profilesPresentationAssembly = typeof(Y.Profiles.Presentation.AssemblyReference).Assembly;
    var articlesPresentationAssembly = typeof(Y.Articles.Presentation.AssemblyReference).Assembly;
    var threadsPresentationAssembly = typeof(Y.Threads.Presentation.AssemblyReference).Assembly;

    builder.Services
        .AddControllers()
        .AddApplicationPart(profilesPresentationAssembly)
        .AddApplicationPart(articlesPresentationAssembly)
        .AddApplicationPart(threadsPresentationAssembly);

    builder.Services
        .AddAuthorization()
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]!)),
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

    app.UseHttpsRedirection();

    app.UseAuthentication();

    app.UseAuthorization();

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "An unhandled exception occurred during bootstrapping");
}
finally
{
    Log.CloseAndFlush();
}

void ThreadsDependencyInjection()
{
    Y.Threads.Infrastructure.DependencyInjection.AddInfrastructure(builder.Services, builder.Configuration);
    Y.Threads.Application.DependencyInjection.AddApplication(builder.Services, builder.Configuration);
}
