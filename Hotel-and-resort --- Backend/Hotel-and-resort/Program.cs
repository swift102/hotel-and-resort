using AspNetCoreRateLimit;
using hotel_and_resort.Models;
using hotel_and_resort.Services;
using Hotel_and_resort.Models;
using Hotel_and_resort.Services;
using Hotel_and_resort.Data;
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

// Add services to the container 
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
builder.Services.AddIdentity<User, IdentityRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = true; // Changed to true for better security
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false; // Set to true in production
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

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

builder.Services.AddControllers();
builder.Services.AddControllersWithViews(); // Added for MVC support
builder.Services.AddRazorPages(); // Added for Razor Pages support
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
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();

// Middleware order is important!
app.UseIpRateLimiting();

// Custom middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<AuditLoggingMiddleware>();

app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Map controllers and routes
app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

// Initialize Database with Enhanced Seeding
await InitializeDatabaseAsync(app);

app.Run();

// Enhanced Database initialization method
async Task InitializeDatabaseAsync(WebApplication app)
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;

        try
        {
            var context = services.GetRequiredService<AppDbContext>();
            var userManager = services.GetRequiredService<UserManager<User>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var logger = services.GetRequiredService<ILogger<Program>>();

            logger.LogInformation("Starting database initialization...");

            // Apply migrations
            logger.LogInformation("Applying database migrations...");
            await context.Database.MigrateAsync();
            logger.LogInformation("Database migrations completed successfully.");

            // Initialize with seed data using DbInitializer
            logger.LogInformation("Starting database seeding...");
            await DbInitializer.InitializeAsync(context, userManager, roleManager, logger);
            logger.LogInformation("Database seeding completed successfully.");

            // Optional: Additional seeding from SeedData if you have it
            
            logger.LogInformation("Seeding additional user data...");
            foreach (var user in SeedData.GetUsers())
            {
                var existingUser = await userManager.FindByEmailAsync(user.Email);
                if (existingUser == null)
                {
                    var result = await userManager.CreateAsync(user, "DefaultPassword123!");
                    if (result.Succeeded)
                    {
                        logger.LogInformation($"User {user.Email} created successfully.");
                    }
                    else
                    {
                        logger.LogWarning($"Failed to create user {user.Email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }
                }
            }
            

        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred while initializing the database.");

            // In development, you might want to throw the exception to see the full error
            if (app.Environment.IsDevelopment())
            {
                throw;
            }

           
        }
    }
}