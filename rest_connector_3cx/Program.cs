using Chat_3CX_API.Models;
using log4net;
using log4net.Config;
using Microsoft.OpenApi.Models;
using System.Net;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Log4net configuration
var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));

builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<ConnectionHandler>();
builder.Services.AddSingleton<ChatSessionStorage>();
builder.Services.AddHostedService<ChatStatusCheckerService>();

var restConnectorApiSettings = builder.Configuration.GetSection("RestConnectorApiSettings");
var restConnectorApiIp = restConnectorApiSettings["RestConnectorIp"];
if (restConnectorApiIp == null)
{
    throw new Exception("No ConnectorApi IP address defined in appsettings.");
}
var restConnectorApiPort = restConnectorApiSettings.GetValue<int>("RestConnectorPort");
var swaggerEnabled = restConnectorApiSettings["Swagger_enabled"];
var swaggerPass = restConnectorApiSettings["Swagger_password"];
var trustedPbx = restConnectorApiSettings["TrustedPbx"];
if (trustedPbx == null)
{
    throw new Exception("No PBX address defined in appsettings.");
}

builder.WebHost.UseKestrel(options =>
{
    options.Listen(IPAddress.Parse(restConnectorApiIp), restConnectorApiPort);
});

Console.WriteLine($"Listening on IP: {restConnectorApiIp}:{restConnectorApiPort}");

if (swaggerEnabled == "true")
{
    // Swagger configuration
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Version = "v1",
            Title = "REST Connector API",
            Description = "API documentation for REST Connector",
        });

        // Register the operation filter
        c.OperationFilter<AddAuthorizationHeader>();

        // Enable the security definition
        var securitySchema = new OpenApiSecurityScheme
        {
            Description = "Enter the Swagger password in the text input below.",
            Name = "Swagger_password",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
        };

        //c.AddSecurityDefinition("Swagger_password", securitySchema);

        // Apply this security to all endpoints
        c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { securitySchema, Array.Empty<string>() }
    });

        c.EnableAnnotations();
    });

}

var app = builder.Build();

if (swaggerEnabled == "true")
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "REST Connector API V1");
        c.RoutePrefix = "swagger";
        c.DisplayOperationId();
        c.DefaultModelsExpandDepth(-1);
    });

    var trustedIPs = new List<string> { "pbx.test1.com", "pbx.test2.cz" };

    app.Use(async (context, next) =>
    {
        var remoteIpAddress = context.Connection.RemoteIpAddress?.ToString();
        if (remoteIpAddress == null)
        {
            throw new Exception("No remote IP address");
        }

        // Check if the request is from a trusted IP
        if (!context.Request.Path.StartsWithSegments("/swagger") ||
        (trustedPbx.ToString() == remoteIpAddress.ToString() && remoteIpAddress != null))
        {
            await next.Invoke(); // Skip password check for trusted IP or swagger endpoints
            return;
        }

        // Normal password check
        var swaggerPassFromHeader = context.Request.Headers["Swagger_password"].ToString();
        Console.WriteLine($"swaggerPassFromHeader: {swaggerPassFromHeader}");

        if (string.IsNullOrEmpty(swaggerPassFromHeader) || swaggerPassFromHeader != swaggerPass)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            await context.Response.WriteAsync("Unauthorized: Invalid Swagger password.");
            return;
        }

        await next.Invoke();
    });

}

app.MapControllers();

app.Run();
