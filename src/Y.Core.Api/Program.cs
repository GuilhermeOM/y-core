using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Y.Threads.Application;
using Y.Threads.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

Y.Threads.Infrastructure.DependencyInjection.AddInfrastructure(builder.Services, builder.Configuration);

ThreadsDependencyInjection();

var profilesAssembly = typeof(Y.Profiles.Presentation.AssemblyReference).Assembly;
var articlesAssembly = typeof(Y.Articles.Presentation.AssemblyReference).Assembly;
var threadsAssembly = typeof(Y.Threads.Presentation.AssemblyReference).Assembly;

builder.Services
    .AddControllers()
    .AddApplicationPart(profilesAssembly)
    .AddApplicationPart(articlesAssembly)
    .AddApplicationPart(threadsAssembly);

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

void ThreadsDependencyInjection()
{
    builder.Services
        .AddApplication(builder.Configuration)
        .AddInfrastructure(builder.Configuration);
}
