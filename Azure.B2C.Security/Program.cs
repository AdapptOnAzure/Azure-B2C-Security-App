using Azure.B2C.Security;

var badDomains = File.ReadLines("../bad-domains.txt").ToHashSet();

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders().AddConsole();

builder.Services.AddKeyedSingleton("BadDomains", badDomains);
builder.Services.AddTransient<ILogger>(p =>
{
    var loggerFactory = p.GetRequiredService<ILoggerFactory>();
    return loggerFactory.CreateLogger("Azure.B2C.Security");
});

// Disable the "Server" Header on HTTP responses
builder.WebHost.ConfigureKestrel(options => 
{
    options.AddServerHeader = false;
});

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.MapSignUp();

app.Run();