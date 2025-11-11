using Microsoft.Extensions.Hosting;
using Y.Threads.Infrastructure.Background;

var builder = WebApplication.CreateBuilder(args);

Y.Threads.Infrastructure.DependencyInjection.AddInfrastructure(builder.Services, builder.Configuration);

var profilesAssembly = typeof(Y.Profiles.Presentation.AssemblyReference).Assembly;
var articlesAssembly = typeof(Y.Articles.Presentation.AssemblyReference).Assembly;
var threadsAssembly = typeof(Y.Threads.Presentation.AssemblyReference).Assembly;

builder.Services
    .AddControllers()
    .AddApplicationPart(profilesAssembly)
    .AddApplicationPart(articlesAssembly)
    .AddApplicationPart(threadsAssembly);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
