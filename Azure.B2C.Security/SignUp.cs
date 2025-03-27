using System.Text.Json;
using System.Net.Mail;

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

   private static async ValueTask<IResult> BadEmail(HttpContext context)
   {
      var user = await JsonSerializer.DeserializeAsync<Dictionary<string, JsonElement?>>(context.Request.Body);

      if (user is null 
          || !user.TryGetValue("email", out JsonElement? emailObj)
          || emailObj is not { ValueKind: JsonValueKind.String }
          || !MailAddress.TryCreate(emailObj.Value.ToString(), out MailAddress? email)
          )
      {
         return TypedResults.BadRequest();
      }

      return TypedResults.Ok(new Response());
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