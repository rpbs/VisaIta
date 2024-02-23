using System.Net;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.WebHost
    .UseKestrel()
    .UseUrls("http://*:80")
    .UseIISIntegration();

builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseAuthorization();

app.MapControllers();

app.Run();
