using System.Text.Json;

namespace Azure.B2C.Security;

public static class SignUp
{
    private const string Version = "1.0.0";

    private static HttpClient _httpClient = new()
    {
        BaseAddress = new Uri("https://www.google.com/recaptcha/api/siteverify"),
        Timeout = TimeSpan.FromSeconds(15),
    };

    public static IEndpointRouteBuilder MapSignUp(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapGroup("/signup");

        group.MapPost("/bad-email", BadEmail);
        group.MapPost("/bad-host", BadHost);
        group.MapPost("/too-many-users", TooManyUsers);
        group.MapPost("/captcha", Captcha);

        return endpoints;
    }

    private static async ValueTask<IResult> BadEmail(
        HttpContext context,
        [FromKeyedServices("BadDomains")] HashSet<string> badDomains
    )
    {
        var user = await JsonSerializer.DeserializeAsync<Dictionary<string, JsonElement?>>(
            context.Request.Body
        );

        if (
            user is null
            || !user.TryGetValue("email", out JsonElement? email)
            || email is not { ValueKind: JsonValueKind.String }
            || !email.Value.ToString().Contains('@')
        )
        {
            return TypedResults.BadRequest();
        }

        string[] dnsParts = email.Value.ToString().Split('@')[1].Split('.');

        for (int i = dnsParts.Length - 1; i >= 0; i--)
        {
            string dsn = string.Join('.', dnsParts, i, dnsParts.Length - i);
            if (badDomains.Contains(dsn))
            {
                return TypedResults.Ok(new { Version, Action = Action.ShowBlockPage.ToString() });
            }
        }

        return TypedResults.Ok(new { Version, Action = Action.Continue.ToString() });
    }

    private static IResult BadHost(HttpContext context)
    {
        return TypedResults.Ok();
    }

    private static IResult TooManyUsers(HttpContext context)
    {
        return TypedResults.Ok();
    }

    private static async ValueTask<IResult> Captcha(HttpContext context, IConfiguration config)
    {
        var user = await JsonSerializer.DeserializeAsync<Dictionary<string, JsonElement?>>(
            context.Request.Body
        );

        if (user is null)
        {
            return TypedResults.BadRequest();
        }

        string key = $"extension_{config["AppId"]}_CaptchaUserResponseToken";

        if (
            !user.TryGetValue(key, out JsonElement? token)
            || token is not { ValueKind: JsonValueKind.String }
        )
        {
            return TypedResults.BadRequest(
                new
                {
                    Version,
                    Status = 400,
                    Action = Action.ValidationError.ToString(),
                    UserMessage = "Please complete the Captcha.",
                }
            );
        }

        if (!await RunCaptcha(token.Value.ToString(), config))
        {
            return TypedResults.BadRequest(
                new
                {
                    Version,
                    Status = 400,
                    Action = Action.ValidationError.ToString(),
                    UserMessage = "Captcha validation failed. Please try again.",
                }
            );
        }

        return TypedResults.Ok(new {Version, Action = Action.Continue.ToString()});
    }

    private static async ValueTask<bool> RunCaptcha(string token, IConfiguration config)
    {
        try
        {
            HttpResponseMessage response = await _httpClient.PostAsJsonAsync(
                (string?)null,
                new { Secret = config["CaptchaSecret"], Response = token }
            );
            response.EnsureSuccessStatusCode();

            var data = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement?>>();

            return data is not null && data.TryGetValue("success", out JsonElement? successObj) &&
                   successObj is not null && bool.TryParse(successObj.Value.ToString(), out bool success) && success;
        }
        catch
        {
            return false;
        }
    }
}

public enum Action
{
    ShowBlockPage,
    Continue,
    ValidationError,
}
