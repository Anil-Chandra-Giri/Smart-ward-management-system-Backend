using Domain.Enumerators;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Smart_ward_management_system.Common;
using Smart_ward_management_system.Controllers;
using Smart_ward_management_system.Data;
using Smart_ward_management_system.Filters;
using Smart_ward_management_system.Model.WasteManagement_And_Scheduling;
using Smart_ward_management_system.Services;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<LoggingFilter>(); // Add automatic logging filter
});

builder.Services.AddScoped<ILoggingService, LoggingService>();
builder.Services.AddHttpContextAccessor();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DbString"));
});
builder.Services.AddScoped<DocumentService>();
builder.Services.AddSignalR();
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(
        new JsonNumberEnumConverter<ApprovalStatusEnum>()
    );

    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
}); ;
builder.Services.AddCors(o => o.AddPolicy("MyPolicy", builder =>
{
    builder.AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader();
}));
builder.Services.AddSignalR();
builder.Services.AddHttpClient<ISmsService, SparrowSmsService>();
builder.Services.AddScoped<ISmsService, SparrowSmsService>();
builder.Services.AddScoped<IFollowUpService, FollowUpService>();
builder.Services.AddScoped<IEmailService, EmailService>(); // You need to implement this
builder.Services.AddScoped<INotificationService, NotificationService>(); // You need to implement this

// Add background service
builder.Services.AddHostedService<FollowUpBackgroundService>();

// Add services to the container.
var jwtSecret = builder.Configuration.GetValue<string>("JWT:Key");
var keyBytes = Encoding.ASCII.GetBytes(jwtSecret);

// Add services to the container.
builder.Services.AddAuthentication(O =>
{
    O.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    O.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    O.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    O.RequireAuthenticatedSignIn = false;
}).AddJwtBearer(Options =>
{
    Options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidIssuer = "localhost:7069",
        ValidAudience = "localhost:4200",
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,
        NameClaimType = ClaimTypes.NameIdentifier
    };

    Options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            // Swagger UI is usually accessed from a different port
            if (context.Request.Path.StartsWithSegments("/swagger"))
            {
                context.NoResult();
            }
            return Task.CompletedTask;
        }
    };
});



// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// Configure file upload limits
builder.Services.Configure<FormOptions>(options =>
{
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartBodyLengthLimit = int.MaxValue;
    options.MemoryBufferThreshold = int.MaxValue;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors("MyPolicy");
app.MapHub<QueueHub>("/queueHub");

app.UseAuthorization();

app.MapControllers();
app.MapHub<RealTimeHub>("/realtimeHub");
app.Run();
