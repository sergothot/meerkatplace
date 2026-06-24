using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddHttpLogging(options =>
{
	options.LoggingFields = HttpLoggingFields.RequestMethod |
							HttpLoggingFields.RequestPath |
							HttpLoggingFields.ResponseStatusCode |
							HttpLoggingFields.Duration;
	options.RequestHeaders.Add("X-Correlation-Id");
	options.ResponseHeaders.Add("X-Correlation-Id");
});

var jwtKey = builder.Configuration["Jwt:Key"] ?? string.Empty;
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? string.Empty;
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? string.Empty;

builder.Services
	.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
	.AddJwtBearer(options =>
	{
		options.TokenValidationParameters = new TokenValidationParameters
		{
			ValidateIssuer = true,
			ValidateAudience = true,
			ValidateLifetime = true,
			ValidateIssuerSigningKey = true,
			ValidIssuer = jwtIssuer,
			ValidAudience = jwtAudience,
			IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
		};
	});

builder.Services.AddAuthorization(options =>
{
	options.AddPolicy("authenticated", policy => policy.RequireAuthenticatedUser());
});

builder.Services.AddRateLimiter(options =>
{
	options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
	options.AddPolicy("api", context =>
	{
		var partitionKey = context.User.Identity?.IsAuthenticated == true
			? context.User.Identity.Name ?? context.User.FindFirst("sub")?.Value ?? "authenticated"
			: context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";

		return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
		{
			PermitLimit = builder.Configuration.GetValue<int>("RateLimiting:PermitLimit", 100),
			Window = TimeSpan.FromSeconds(builder.Configuration.GetValue<int>("RateLimiting:WindowSeconds", 60)),
			QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
			QueueLimit = builder.Configuration.GetValue<int>("RateLimiting:QueueLimit", 20)
		});
	});
});

var app = builder.Build();

app.UseHttpLogging();

app.Use(async (context, next) =>
{
	const string headerName = "X-Correlation-Id";

	if (!context.Request.Headers.TryGetValue(headerName, out var correlationId) || string.IsNullOrWhiteSpace(correlationId))
	{
		correlationId = Guid.NewGuid().ToString("N");
		context.Request.Headers[headerName] = correlationId;
	}

	context.Response.Headers[headerName] = correlationId.ToString();
	context.TraceIdentifier = correlationId.ToString();

	await next();
});

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => "Hello World!");
app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "api-gateway" }));
app.MapGet("/docs", () => Results.Content(GetGatewayDocsHtml(), "text/html"));
app.MapGet("/docs/demo", () => Results.Content(GetGatewayDemoHtml(), "text/html"));
app.MapReverseProxy().RequireRateLimiting("api");

app.Run();

static string GetGatewayDocsHtml()
{
		return """
<!doctype html>
<html lang="en">
<head>
	<meta charset="utf-8" />
	<meta name="viewport" content="width=device-width, initial-scale=1" />
	<title>Meerkatplace API Docs</title>
	<style>
		body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif; margin: 2rem; line-height: 1.5; }
		h1 { margin-bottom: 0.5rem; }
		ul { padding-left: 1.2rem; }
		li { margin: 0.4rem 0; }
		code { background: #f5f5f5; padding: 0.1rem 0.3rem; border-radius: 4px; }
	</style>
</head>
<body>
	<h1>Meerkatplace API Docs</h1>
	<ul>
		<li><a href="/docs/demo">Demo Walkthrough</a> (<code>http://localhost:8080/docs/demo</code>)</li>
		<li><a href="http://localhost:5001/docs" target="_blank" rel="noopener">User Service Docs</a> (<code>http://localhost:5001/docs</code>)</li>
		<li><a href="http://localhost:5002/docs" target="_blank" rel="noopener">Listing Service Docs</a> (<code>http://localhost:5002/docs</code>)</li>
		<li><a href="http://localhost:5003/docs" target="_blank" rel="noopener">Order Service Docs</a> (<code>http://localhost:5003/docs</code>)</li>
		<li><a href="http://localhost:5004/docs" target="_blank" rel="noopener">Payment Service Docs</a> (<code>http://localhost:5004/docs</code>)</li>
		<li><a href="http://localhost:8080/health" target="_blank" rel="noopener">Gateway Health</a> (<code>http://localhost:8080/health</code>)</li>
	</ul>
</body>
</html>
""";
}

static string GetGatewayDemoHtml()
{
	return """
<!doctype html>
<html lang="en">
<head>
	<meta charset="utf-8" />
	<meta name="viewport" content="width=device-width, initial-scale=1" />
	<title>Meerkatplace Demo Walkthrough</title>
	<style>
		body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif; margin: 2rem; line-height: 1.5; }
		h1 { margin-bottom: 0.5rem; }
		code { background: #f5f5f5; padding: 0.1rem 0.3rem; border-radius: 4px; }
		pre { background: #111; color: #eee; padding: 0.8rem; border-radius: 8px; overflow-x: auto; }
	</style>
</head>
<body>
	<h1>Meerkatplace Demo</h1>

	<h2>Docs Pages</h2>
	<ul>
		<li><a href="http://localhost:5001/docs" target="_blank" rel="noopener">User docs</a></li>
		<li><a href="http://localhost:5002/docs" target="_blank" rel="noopener">Listing docs</a></li>
		<li><a href="http://localhost:5003/docs" target="_blank" rel="noopener">Order docs</a></li>
		<li><a href="http://localhost:5004/docs" target="_blank" rel="noopener">Payment docs</a></li>
	</ul>

	<h2>Quick Flow</h2>
	<ol>
		<li>Register + Login via <code>/api/v1/auth/*</code> to get access token.</li>
		<li>Create product via <code>/api/v1/listing/products</code>.</li>
		<li>Add cart item + checkout via <code>/api/v1/order/cart/*</code> with <code>X-User-Id</code>.</li>
		<li>Poll order status via <code>/api/v1/order/orders/{orderId}/status</code>.</li>
		<li>View shipment via <code>/api/v1/order/orders/{orderId}/shipments</code>.</li>
	</ol>

	<h2>Sample Request Payloads</h2>
	<pre>{
  "login": "demo_user_001",
  "email": "demo_user_001@example.com",
  "password": "Password123!"
}</pre>

	<pre>{
  "sellerId": "11111111-1111-1111-1111-111111111111",
  "name": "Demo Mouse",
  "description": "Demo product for walkthrough",
  "price": 1499.99,
  "currency": "RUB",
  "deliveryType": "Physical",
  "stockQuantity": 5
}</pre>

	<pre>{
  "productId": "{productId}",
  "quantity": 1,
  "unitPrice": 1499.99,
  "currency": "RUB"
}</pre>

	<pre>{
  "addressId": "demo-address",
  "paymentMethod": "Wallet"
}</pre>
</body>
</html>
""";
}
