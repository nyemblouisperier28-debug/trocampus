using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Distributed;
using Npgsql;
using System.Threading.RateLimiting;
using Trocampus.Api.Endpoints;
using Trocampus.Api.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default")
	?? throw new InvalidOperationException("ConnectionStrings:Default is required.");
var redisConnection = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";

builder.Services.AddOpenApi();
builder.Services.AddValidation();
builder.Services.AddProblemDetails();
builder.Services.AddSingleton(NpgsqlDataSource.Create(connectionString));
builder.Services.AddStackExchangeRedisCache(options =>
{
	options.Configuration = redisConnection;
	options.InstanceName = "trocampus:";
});
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ListingService>();
builder.Services.AddRateLimiter(options =>
{
	options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
	options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
		context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
		_ => new FixedWindowRateLimiterOptions
		{
			PermitLimit = 10,
			Window = TimeSpan.FromMinutes(1),
			QueueLimit = 0
		}));
});

var app = builder.Build();
app.UseExceptionHandler();
app.UseRateLimiter();
app.MapOpenApi();
app.MapHealthEndpoints();
app.MapAuthEndpoints();
app.MapListingEndpoints();
app.MapHomeEndpoints();
app.MapActivityEndpoints();

app.Run();

public partial class Program;
