using Azure.Identity;
using Microsoft.Graph;

namespace Azure.B2C.Security.Utils;

public class DynamicGraphClientFactory(IConfiguration config)
{
    public HttpClient Create(string tenantId)
    {
        string id = tenantId.Replace('-', '_');
        string clientId = config[$"GRAPHAPI_{id}_CLIENTID"]!;
        string clientSecret = config[$"GRAPHAPI_{id}_CLIENTSECRET"]!;

        var creds = new ClientSecretCredential(tenantId, clientId, clientSecret, new ClientSecretCredentialOptions()
        {
            AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
        });
        return GraphClientFactory.Create(creds, version: "beta");
    }
}