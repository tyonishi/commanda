using System.Runtime.Versioning;
using Commanda.Core;
using Commanda.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

[assembly: SupportedOSPlatform("windows")]

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register Commanda services
builder.Services.AddSingleton<ISettingsManager, SettingsManager>();
builder.Services.AddSingleton<IExtensionManager, ExtensionManager>();
builder.Services.AddSingleton<ILlmProviderManager, LlmProviderManager>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();