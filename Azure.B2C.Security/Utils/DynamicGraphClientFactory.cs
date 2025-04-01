using Azure.Identity;
using Microsoft.Graph;

namespace Azure.B2C.Security.Utils;

public class DynamicGraphClientFactory(IConfiguration config)
{
    public HttpClient Create(string tenantId)
    {
        string clientId = config[$"GraphAPI:{tenantId}:ClientId"]!;
        string clientSecret = config[$"GraphAPI:{tenantId}:ClientSecret"]!;

        var creds = new ClientSecretCredential(tenantId, clientId, clientSecret, new ClientSecretCredentialOptions()
        {
            AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
        });
        return GraphClientFactory.Create(creds, version: "beta");
    }
}