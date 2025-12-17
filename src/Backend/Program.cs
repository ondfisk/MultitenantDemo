var myAllowSpecificOrigins = "MyAllowSpecificOrigins";

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));
builder.Services.AddAuthorization();
builder.Services.AddHealthChecks();

if (!builder.Environment.IsDevelopment())
{
    builder.Services.AddOpenTelemetry().UseAzureMonitor();
}

builder.Services.AddCors(options =>
{
    var origins = builder.Configuration["Cors:AllowedOrigins"]?.Split(',') ?? [];

    options.AddPolicy(name: myAllowSpecificOrigins,
                      policy  =>
                      {
                          policy.WithOrigins(origins).AllowAnyMethod().AllowAnyHeader();
                      });
});

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors(myAllowSpecificOrigins);

app.MapOpenApi();
app.MapHealthChecks("/healthz");

var scopes = builder.Configuration["AzureAd:Scopes"]?.Split(',') ?? [];
var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", (HttpContext httpContext) =>
{
    httpContext.VerifyUserHasAnyAcceptedScope(scopes);
    httpContext.ValidateAppRole("Customer");

    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.RequireAuthorization();

app.MapGet("/claims", (HttpContext httpContext) =>
{
    httpContext.VerifyUserHasAnyAcceptedScope(scopes);

    return httpContext.User.Claims.Select(c => new Claim(c.Type, c.Value));
})
.WithName("GetClaims")
.RequireAuthorization();

app.Run();

public partial class Program { }
