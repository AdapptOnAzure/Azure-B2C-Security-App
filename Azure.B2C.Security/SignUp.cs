using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace Azure.B2C.Security;

public static class SignUp
{
   public static IEndpointRouteBuilder MapSignUp(this IEndpointRouteBuilder endpoints)
   {
      RouteGroupBuilder group = endpoints.MapGroup("/signup");

      group.MapPost("/bad-email", BadEmail).WithOpenApi();

      return endpoints;
   }

   private static IResult BadEmail([FromBody] User user)
   {
      return TypedResults.BadRequest();
   }
}