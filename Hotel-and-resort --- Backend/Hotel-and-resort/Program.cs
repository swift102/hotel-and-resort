using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using hotel_and_resort.Models;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.FileProviders;
using System.IO;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);


// Add services to the container.
// Register your repository
builder.Services.AddScoped<IRepository, Repository>();

// Add services to the container.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions =>
        {
            sqlOptions.CommandTimeout(180); // Set 180 seconds timeout
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,              // Maximum retry attempts
                maxRetryDelay: TimeSpan.FromSeconds(30),  // Max delay between retries
                errorNumbersToAdd: null        // Retry on all transient errors
            );
        })
    .EnableSensitiveDataLogging() // Enable detailed logging
    .LogTo(Console.WriteLine, LogLevel.Information) // Log to console
);


builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseStaticFiles(); // Ensure this is added before app.UseRouting()

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
