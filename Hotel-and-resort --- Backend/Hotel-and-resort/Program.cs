using AspNetCoreRateLimit;
using hotel_and_resort.Models;
using hotel_and_resort.Services;
using Hotel_and_resort.Models;
using Hotel_and_resort.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc; 
using Microsoft.AspNetCore.Mvc.Versioning; 
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Middleware;
using Stripe;
using System.Text;
using System.Threading.RateLimiting;






var builder = WebApplication.CreateBuilder(args);

// Add rate limiting services with proper configuration
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

// Rate limiting stores
builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();

// Rate limiting configuration - use the correct service registration
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

// Configure rate limiting options
builder.Services.Configure<IpRateLimitOptions>(options =>
{
    builder.Configuration.GetSection("IpRateLimiting").Bind(options);
});

// Configure client rate limiting if needed
builder.Services.Configure<ClientRateLimitOptions>(options =>
{
    builder.Configuration.GetSection("ClientRateLimiting").Bind(options);
});


builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("AuthPolicy", context => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));
});



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

// Register services
builder.Services.AddScoped<IRepository, Repository>();
builder.Services.AddScoped<RoomService>();
builder.Services.AddScoped<AmenityService>();
builder.Services.AddScoped<PricingService>();
builder.Services.AddScoped<Hotel_and_resort.Services.CustomerService>();
builder.Services.AddHttpClient<PaymentService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<hotel_and_resort.Services.TokenService>();
builder.Services.AddScoped<PaymentIntentService>(provider =>
    new PaymentIntentService(new StripeClient(builder.Configuration["Stripe:SecretKey"])));


// Register RabbitMQ Event Publisher
builder.Services.AddSingleton<IRabbitMqEventPublisher, RabbitMqEventPublisher>();



// Configure SmtpSettings
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));


builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "HotelResort_";
});

// Add HTTPS redirection
builder.Services.AddHttpsRedirection(options =>
{
    options.RedirectStatusCode = StatusCodes.Status307TemporaryRedirect;
    options.HttpsPort = 443;
});

// Add HSTS
builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);
    options.IncludeSubDomains = true;
    options.Preload = true;
});




// Add API versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();
});

// Register EmailSender for both interfaces
builder.Services.AddSingleton<hotel_and_resort.Services.IEmailSender, EmailSender>();
builder.Services.AddSingleton<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender, EmailSender>();

// Configure Stripe
StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];
builder.Services.AddSingleton<PaymentIntentService>(new PaymentIntentService(new StripeClient(builder.Configuration["Stripe:SecretKey"])));

// Register SMS Sender
builder.Services.AddScoped<ISmsSenderService>(provider =>
{
    var logger = provider.GetRequiredService<ILogger<SmsSenderService>>();
    return new SmsSenderService("59cb8814", "ttNnQ3C9ZQtxM75H", logger);
});

builder.Services.AddIdentity<User, IdentityRole>()
      .AddEntityFrameworkStores<AppDbContext>()
      .AddDefaultTokenProviders();


builder.Services.AddControllers();
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
    app.UseStaticFiles(); 
}

// After building the app
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    // Seed using the seeder class
    var seeder = new DatabaseSeeder(context, userManager, roleManager);
    await seeder.SeedAsync();
}

// User seeding separately
using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

    foreach (var user in SeedData.GetUsers())
    {
        await userManager.CreateAsync(user, "DefaultPassword123!");
    }
}

// Middleware order is important!
app.UseIpRateLimiting(); 

// Custom middleware
app.UseMiddleware<ExceptionHandlingMiddleware>(); 
app.UseMiddleware<AuditLoggingMiddleware>();


app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();