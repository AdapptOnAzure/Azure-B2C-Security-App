using Azure.B2C.Security;

var badDomains = File.ReadLines("../bad-domains.txt").ToHashSet();

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddKeyedSingleton("BadDomains", badDomains);

var app = builder.Build();

app.UseHttpsRedirection();

app.MapSignUp();

app.Run();