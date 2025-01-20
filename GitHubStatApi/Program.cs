using GitHubStatApi.Configuration;
using GitHubStatApi.Interfaces;
using GitHubStatApi.Services;
using GitHubStatApi.Utils;
using Microsoft.AspNetCore.Diagnostics;
using static System.Net.Mime.MediaTypeNames;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IFileContentAnalyzerService, FileContentAnalyzerService>();
builder.Services.AddScoped<IGitHubRepoService, GitHubRepoService>();

builder.Services.AddSingleton<IPolicyFactory, PolicyFactory>();
builder.Services.AddSingleton<IGitHubClientFactory, GitHubClientFactory>();

builder.Services.AddHttpClient();

builder.Services.Configure<GitHubOptions>(builder.Configuration.GetSection(nameof(GitHubOptions)));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        var exceptionHandlerPathFeature =
            context.Features.Get<IExceptionHandlerPathFeature>();

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        if (exceptionHandlerPathFeature?.Error is OperationCanceledException)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
        }

        context.Response.ContentType = Application.Json;
        await context.Response.WriteAsJsonAsync(new
        {
            exceptionHandlerPathFeature?.Error?.Message,
            StackTrace = app.Environment.IsDevelopment() ? exceptionHandlerPathFeature?.Error?.StackTrace : null
        });
    });
});

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }