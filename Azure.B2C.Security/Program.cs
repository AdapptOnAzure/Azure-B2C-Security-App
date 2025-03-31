using Azure.B2C.Security;

var badDomains = File.ReadLines("../bad-domains.txt").ToHashSet();

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders().AddConsole();

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddKeyedSingleton("BadDomains", badDomains);
builder.Services.AddTransient<ILogger>(p =>
{
    var loggerFactory = p.GetRequiredService<ILoggerFactory>();
    return loggerFactory.CreateLogger("Azure.B2C.Security");
});

var app = builder.Build();

app.UseHttpsRedirection();

app.MapSignUp();

app.Run();