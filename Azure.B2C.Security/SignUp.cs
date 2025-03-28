using System.Text.Json;

namespace Azure.B2C.Security;

public static class SignUp
{
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
                return TypedResults.Ok(new Response(Action: Action.ShowBlockPage.ToString()));
            }
        }

        return TypedResults.Ok(new Response(Action: Action.Continue.ToString()));
    }

    private static IResult BadHost(HttpContext context)
    {
        string remoteIp = context.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
        return TypedResults.Ok(new Response());
    }

    private static IResult TooManyUsers(HttpContext context)
    {
        return TypedResults.Ok(new Response());
    }
}

public enum Action
{
    ShowBlockPage,
    Continue,
}

public record Response(string Version = "1.0.0", string Action = "ShowBlockPage");
