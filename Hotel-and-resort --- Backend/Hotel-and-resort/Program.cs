using hotel_and_resort.Models;
using hotel_and_resort.Services;
using Hotel_and_resort.Models;
using Hotel_and_resort.Services;
using Hotel_and_resort.Services.hotel_and_resort.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.UI;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Stripe;
using System.IO;
using System.Text;
using System.Text.Json.Serialization;
using Vonage;
using Vonage.Request;

var builder = WebApplication.CreateBuilder(args);

// Add Stripe configuration
StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];

// Add JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.ASCII.GetBytes(jwtSettings["Key"]);


builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});


builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });


// Add services to the container.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions =>
        {
            sqlOptions.CommandTimeout(180);
            sqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(30), null);
        })
    .LogTo(Console.WriteLine, LogLevel.Information)
    .EnableSensitiveDataLogging(builder.Environment.IsDevelopment()));


// Add Identity services
builder.Services.AddIdentity<User, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();




// Register your repository
builder.Services.AddScoped<IRepository, Repository>();
builder.Services.AddScoped<IEmailSender, EmailSender>();
builder.Services.AddSingleton<IEmailSender, EmailSender>();
builder.Services.AddScoped<RoomService>();
builder.Services.AddScoped<AmenityService>();
builder.Services.AddScoped<PricingService>();
//builder.Services.AddScoped<CustomerService>();
builder.Services.AddHttpClient<PaymentService>();
builder.Services.AddScoped<PaymentService>();

builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));
builder.Services.AddSingleton(resolver => resolver.GetRequiredService<IOptions<SmtpSettings>>().Value);
builder.Services.AddSingleton(builder.Configuration);


var service = new PaymentIntentService(new StripeClient(builder.Configuration["Stripe:SecretKey"]));

StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];

builder.Services.AddScoped<ISmsSenderService>(provider =>
{
    var logger = provider.GetRequiredService<ILogger<SmsSenderService>>();
    return new SmsSenderService("59cb8814", "ttNnQ3C9ZQtxM75H", logger);
});




builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Hotel API", Version = "v1" });
});

builder.Services.AddAuthorization();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();