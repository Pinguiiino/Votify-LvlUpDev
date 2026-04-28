using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace Votify.Web.Auth;

public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly ProtectedSessionStorage _sessionStorage;

    public CustomAuthStateProvider(ProtectedSessionStorage sessionStorage)
    {
        _sessionStorage = sessionStorage;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            // 1. Intentamos leer los datos del usuario guardados en el navegador
            var userSessionResult = await _sessionStorage.GetAsync<UserSession>("UserSession");
            var userSession = userSessionResult.Success ? userSessionResult.Value : null;

            // 2. Si no hay datos, le decimos a Blazor que el usuario es "Anónimo"
            if (userSession == null)
            {
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }

            // 3. Si hay datos, creamos su "carnet de identidad" (Claims) con su nombre y su rol
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userSession.Id),
                new Claim(ClaimTypes.Name, userSession.Username),
                new Claim(ClaimTypes.Email, userSession.Email),
                new Claim(ClaimTypes.Role, userSession.Role)
            };

            var identity = new ClaimsIdentity(claims, "CustomAuth");
            var principal = new ClaimsPrincipal(identity);

            return new AuthenticationState(principal);
        }
        catch
        {
            // Si hay un error (ej: el usuario limpió el caché), lo tratamos como anónimo
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }
    }

    // Este método lo llamaremos desde la página de Login o Logout para avisar a Blazor del cambio
    public async Task UpdateAuthenticationState(UserSession? userSession)
    {
        ClaimsPrincipal claimsPrincipal;

        if (userSession != null)
        {
            await _sessionStorage.SetAsync("UserSession", userSession);
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userSession.Id),
                new Claim(ClaimTypes.Name, userSession.Username),
                new Claim(ClaimTypes.Email, userSession.Email),
                new Claim(ClaimTypes.Role, userSession.Role)
            };
            var identity = new ClaimsIdentity(claims, "CustomAuth");
            claimsPrincipal = new ClaimsPrincipal(identity);
        }
        else
        {
            await _sessionStorage.DeleteAsync("UserSession");
            claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity());
        }

        // Avisa a toda la aplicación de que el estado ha cambiado
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(claimsPrincipal)));
    }
}

// Una clase sencilla para guardar los datos básicos
public class UserSession
{
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}