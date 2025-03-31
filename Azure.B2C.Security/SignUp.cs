using System.Net.Http.Headers;
using System.Text.Json;

namespace Azure.B2C.Security;

public static class SignUp
{
    private const string Version = "1.0.0";
    private const string BlockingResponseMessage = "There was a problem with your request. You are not able to sign up at this time. Please contact your system administrator";

    private static readonly HttpClient CredentialClient = new()
    {
        BaseAddress = new Uri("https://login.microsoftonline.com"),
        DefaultRequestHeaders = { { "Content-Type", "application/x-www-form-urlencoded" } },
        Timeout = TimeSpan.FromSeconds(15),
    };
    private static readonly HttpClient GraphClient = new()
    {
        BaseAddress = new Uri("https://graph.microsoft.com/beta"),
        Timeout = TimeSpan.FromSeconds(15),
    };

    public static IEndpointRouteBuilder MapSignUp(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapGroup("/signup");

        group.MapGet("/", () => "Hello World from /signup!");

        group.MapPost("/bad-email", BadEmail);
        group.MapPost("/too-many-users", TooManyUsers);

        return endpoints;
    }

    private static async ValueTask<IResult> BadEmail(
        HttpContext context,
        ILogger logger,
        [FromKeyedServices("BadDomains")] HashSet<string> badDomains
    )
    {
        var user = await context.Request.ReadFromJsonAsync<Dictionary<string, JsonElement?>>();

        if (
            user is null
            || !user.TryGetValue("email", out JsonElement? email)
            || email is not { ValueKind: JsonValueKind.String }
            || !email.Value.ToString().Contains('@')
        )
        {
            logger.LogWarning("Bad email format.");
            return TypedResults.BadRequest();
        }

        string[] dnsParts = email.Value.ToString().Split('@')[1].Split('.');

        for (int i = dnsParts.Length - 1; i >= 0; i--)
        {
            string dsn = string.Join('.', dnsParts, i, dnsParts.Length - i);
            if (badDomains.Contains(dsn))
            {
                logger.LogWarning("Bad email domain: {Domain}", dsn);
                return TypedResults.Ok(
                    new
                    {
                        Version,
                        Action = Action.ShowBlockPage.ToString(),
                        UserMessage = BlockingResponseMessage,
                    }
                );
            }
        }

        return TypedResults.Ok(new { Version, Action = Action.Continue.ToString() });
    }

    private static async ValueTask<IResult> TooManyUsers(HttpContext context, IConfiguration config)
    {
        string tenantId =
            config["TenantId"] ?? throw new InvalidOperationException("TenantId is not set.");
        string appId = config["AppId"] ?? throw new InvalidOperationException("AppId is not set.");
        string appSecret =
            config["AppSecret"] ?? throw new InvalidOperationException("AppSecret is not set.");

        HttpResponseMessage response = await CredentialClient.PostAsync(
            new Uri($"{tenantId}/oauth2/v2.0/token"),
            new FormUrlEncodedContent(
                [
                    new KeyValuePair<string, string>("grant_type", "client_credentials"),
                    new KeyValuePair<string, string>("client_id", appId),
                    new KeyValuePair<string, string>("client_secret", appSecret),
                    new KeyValuePair<string, string>(
                        "scope",
                        "https://graph.microsoft.com/.default"
                    ),
                ]
            )
        );
        response.EnsureSuccessStatusCode();
        var tokenData = await response.Content.ReadFromJsonAsync<
            Dictionary<string, JsonElement?>
        >();

        if (
            tokenData is null
            || !tokenData.TryGetValue("access_token", out JsonElement? token)
            || token is not { ValueKind: JsonValueKind.String }
        )
        {
            return TypedResults.BadRequest();
        }

        string accessToken = token.Value.ToString();
        GraphClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            accessToken
        );

        var quotaData = await GraphClient.GetFromJsonAsync<Dictionary<string, JsonElement?>>(
            new Uri("/organization?select=directorySizeQuota")
        );
        if (
            quotaData is null
            || !quotaData.TryGetValue("value", out JsonElement? value)
            || value is not { ValueKind: JsonValueKind.Object }
            || !value.Value.TryGetProperty("directorySizeQuota", out JsonElement quota)
            || quota is not { ValueKind: JsonValueKind.Object }
            || !quota.TryGetProperty("used", out JsonElement used)
            || used is not { ValueKind: JsonValueKind.Number }
        )
        {
            GraphClient.DefaultRequestHeaders.Authorization = null;
            return TypedResults.BadRequest();
        }

        int limit = config.GetValue("QuotaLimit", 50000);

        if (used.GetInt32() < limit)
        {
            return TypedResults.Ok(new { Version, Action = Action.Continue.ToString() });
        }

        // TODO: issue an alert to the admin

        return TypedResults.Ok(new { Version, Action = Action.ShowBlockPage.ToString() });
    }
}

public enum Action
{
    ShowBlockPage,
    Continue,
    ValidationError,
}
