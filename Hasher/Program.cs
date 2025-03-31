using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

var hasher = new PasswordHasher<IdentityUser>(new OptionsWrapper<PasswordHasherOptions>(new PasswordHasherOptions()
{
    IterationCount = 300_000,
}));

Console.WriteLine(hasher.HashPassword(new IdentityUser(), args[0]));