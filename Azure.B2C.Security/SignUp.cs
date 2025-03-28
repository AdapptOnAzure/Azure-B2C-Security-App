using System.Text.Json;

namespace Azure.B2C.Security;

public static class SignUp
{
    private const string Version = "1.0.0";

    public static IEndpointRouteBuilder MapSignUp(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapGroup("/signup");

        group.MapPost("/bad-email", BadEmail);
        group.MapPost("/bad-host", BadHost);
        group.MapPost("/too-many-users", TooManyUsers);

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
}

public enum Action
{
    ShowBlockPage,
    Continue,
    ValidationError,
}
