using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Starter.Api.Data;
using Starter.Api.Features.Teams;
using Starter.Api.Features.Todos;

const string FrontendCorsPolicy = "frontend";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddHttpContextAccessor();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddScoped<TeamContext>();
builder.Services.AddScoped<ITodoRepository, TodoRepository>();
builder.Services.AddScoped<TodoService>();

// The customer works in Swedish time, wherever the server runs (kravspec: "Svensk tid, alltid").
var deadlineTimeZone = TimeZoneInfo.FindSystemTimeZoneById(builder.Configuration["Deadlines:TimeZone"] ?? "Europe/Stockholm");
builder.Services.AddSingleton(sp => new DeadlineCalendar(sp.GetRequiredService<TimeProvider>(), deadlineTimeZone));

var frontendOrigin = builder.Configuration["Frontend:Origin"] ?? "http://localhost:5173";
builder.Services.AddCors(options =>
    options.AddPolicy(FrontendCorsPolicy, policy => policy
        .WithOrigins(frontendOrigin)
        .WithHeaders("Content-Type", "X-Team")
        .WithMethods("GET", "POST", "PUT", "DELETE")));

var searchPermitLimit = builder.Configuration.GetValue("RateLimiting:SearchPermitLimit", 60);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = (context, _) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter = ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();
        }

        return ValueTask.CompletedTask;
    };
    options.AddPolicy("search", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: httpContext.Request.Headers[TeamContext.HeaderName].ToString(),
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = searchPermitLimit,
            Window = TimeSpan.FromMinutes(1),
        }));
});

var app = builder.Build();

app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "DENY";
    headers["Referrer-Policy"] = "no-referrer";
    headers["Cross-Origin-Resource-Policy"] = "same-site";
    await next();
});

using (var scope = app.Services.CreateScope())
{
    // Good enough for a starter. A real project uses EF Core migrations instead.
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors(FrontendCorsPolicy);
app.UseRateLimiter();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();

/// <summary>Exposed so integration tests can start the app with WebApplicationFactory.</summary>
public partial class Program;
