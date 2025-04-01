using Azure.B2C.Security.Controllers;
using Azure.B2C.Security.Utils;

var badDomains = File.ReadLines("bad-domains.txt").ToHashSet();

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders().AddConsole();
builder.Services.AddKeyedSingleton("BadDomains", badDomains);
builder.Services.AddTransient<ILogger>(p =>
{
    var loggerFactory = p.GetRequiredService<ILoggerFactory>();
    return loggerFactory.CreateLogger("Azure.B2C.Security");
});
builder.Services.AddTransient<DynamicGraphClientFactory>();

// Disable the "Server" Header on HTTP responses
builder.WebHost.ConfigureKestrel(options =>
{
    options.AddServerHeader = false;
});

var app = builder.Build();

// Response header middleware
app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        // Yes this is probably overkill and by nature of this application, mostly redundant.
        context.Response.Headers.Append("strict-transport-security", "max-age=63072000; includeSubDomains; preload");
        context.Response.Headers.Append("x-content-type-options", "nosniff");
        context.Response.Headers.Append("referrer-policy", "no-referrer");
        context.Response.Headers.Append("cross-origin-opener-policy", "same-origin");
        context.Response.Headers.Append("cross-origin-embedder-policy", "require-corp");
        context.Response.Headers.Append("cross-origin-resource-policy", "same-origin");
        context.Response.Headers.Append("origin-agent-cluster", "?1");
        context.Response.Headers.Append("x-frame-options", "DENY");
        context.Response.Headers.Append("x-xss-protection", "0");
        context.Response.Headers.Append("content-security-policy", "default-src 'none'; connect-src 'none'; frame-ancestors 'none'; form-action 'none'; sandbox; base-uri 'none'");
        context.Response.Headers.Append("permissions-policy", "accelerometer=(), autoplay=(), bluetooth=(), camera=(), ch-device-memory=(), ch-downlink=(), ch-dpr=(), ch-ect=(), ch-prefers-color-scheme=(), ch-prefers-reduced-motion=(), ch-prefers-reduced-transparency=(), ch-rtt=(), ch-save-data=(), ch-ua=(), ch-ua-arch=(), ch-ua-bitness=(), ch-ua-form-factors=(), ch-ua-full-version=(), ch-ua-full-version-list=(), ch-ua-mobile=(), ch-ua-model=(), ch-ua-platform=(), ch-ua-platform-version=(), ch-ua-wow64=(), ch-viewport-height=(), ch-viewport-width=(), ch-width=(), clipboard-read=(), clipboard-write=(), compute-pressure=(), cross-origin-isolated=(), display-capture=(), encrypted-media=(), ethereum=(), fullscreen=(), geolocation=(), gyroscope=(), hid=(), identity-credentials-get=(), idle-detection=(), keyboard-map=(), local-fonts=(), magnetometer=(), microphone=(), midi=(), payment=(), picture-in-picture=(), publickey-credentials-create=(), publickey-credentials-get=(), screen-wake-lock=(), serial=(), solana=(), storage-access=(), sync-xhr=(), usb=(), web-share=(), window-management=(), xr-spatial-tracking=(), gamepad=(), unload=()");
        return Task.FromResult(0);
    });

    await next();
});

app.MapGet("/", () => "Hello World!");
app.MapPost("/signup/b2c-api-connector", SignUp.OnPost);

app.Run();