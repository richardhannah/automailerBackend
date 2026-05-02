using AutoMailerBackend.Clients;
using AutoMailerBackend.Data;
using AutoMailerBackend.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Database
var dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
var dbPort = Environment.GetEnvironmentVariable("DB_PORT") ?? "5433";
var dbName = Environment.GetEnvironmentVariable("DB_DATABASE") ?? "automailer";
var dbUser = Environment.GetEnvironmentVariable("DB_USERNAME") ?? "automailer";
var dbPass = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "automailer_dev";
var connectionString = $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPass}";
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

// Services
var brevoSettings = new BrevoSettings
{
    ApiKey = Environment.GetEnvironmentVariable("BREVO_API_KEY") ?? "",
    SenderName = Environment.GetEnvironmentVariable("BREVO_SENDER_NAME") ?? "AutoMailer",
    SenderEmail = Environment.GetEnvironmentVariable("BREVO_SENDER_EMAIL") ?? ""
};
builder.Services.AddSingleton(brevoSettings);
builder.Services.AddHttpClient<BrevoClient>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<LoginService>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()
              .SetIsOriginAllowed(_ => true);
    });
});
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo { Title = "AutoMailer API", Version = "v1" });
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "GUID",
        In = Microsoft.OpenApi.ParameterLocation.Header,
        Description = "Enter your token from POST /api/login"
    });
    options.AddSecurityRequirement(doc => new Microsoft.OpenApi.OpenApiSecurityRequirement
    {
        [new Microsoft.OpenApi.OpenApiSecuritySchemeReference("Bearer", doc)] = new List<string>()
    });
});

var app = builder.Build();

// Auto-apply migrations
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Failed to apply migrations");
    }
}

app.UseCors();
app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();
