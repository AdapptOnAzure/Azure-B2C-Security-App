using System.Text.Json;
using Azure.B2C.Security.Utils;

namespace Azure.B2C.Security.Controllers;

public static class SignUp
{
    public static async ValueTask<IResult> OnPost(
        HttpContext context,
        ILogger logger,
        IConfiguration config,
        DynamicGraphClientFactory graphClientFactory,
        [FromKeyedServices("BadDomains")] HashSet<string> badDomains
    )
    {
        var data = await context.Request.ReadFromJsonAsync<JsonDocument>();

        if (data is null)
        {
            logger.LogWarning("Request data is null.");
            return TypedResults.BadRequest();
        }

        #region Bad email domain checks

        if (
            !data.RootElement.TryGetProperty("email", out JsonElement email)
            || email is not { ValueKind: JsonValueKind.String }
            || !email.ToString().Contains('@')
        )
        {
            logger.LogWarning("Bad email format.");
            return TypedResults.BadRequest();
        }

        string[] dnsParts = email.ToString().Split('@')[1].Split('.');

        for (int i = dnsParts.Length - 1; i >= 0; i--)
        {
            string dsn = string.Join('.', dnsParts, i, dnsParts.Length - i);

            if (badDomains.Contains(dsn))
            {
                logger.LogWarning("Bad email domain: {Domain}", dsn);
                return TypedResults.Ok(
                    new
                    {
                        B2CConnector.Version,
                        Action = B2CConnector.Action.ShowBlockPage.ToString(),
                        UserMessage = B2CConnector.BlockingResponseMessage,
                    }
                );
            }
        }

        #endregion

        #region B2C object count checks

        HttpClient graphClient = graphClientFactory.Create(data.RootElement.GetProperty("client_id").GetString()!);
        var response = await graphClient.GetFromJsonAsync<JsonDocument>(
            new Uri("organization?$select=directorySizeQuota", UriKind.Relative)
        );

        int used = response!
            .RootElement.GetProperty("value")[0]
            .GetProperty("directorySizeQuota")
            .GetProperty("used")
            .GetInt32();
        int limit = config.GetValue("QUOTA_LIMIT", 50000);

        if (used > limit)
        {
            // TODO: issue an alert to the admin
            logger.LogWarning("B2C object count limit exceeded: {Used}/{Limit}", used, limit);
            return TypedResults.Ok(
                new
                {
                    B2CConnector.Version,
                    Action = B2CConnector.Action.ShowBlockPage.ToString(),
                    UserMessage = B2CConnector.BlockingResponseMessage,
                }
            );
        }

        #endregion

        return TypedResults.Ok(
            new { B2CConnector.Version, Action = B2CConnector.Action.Continue.ToString() }
        );
    }
}
