using Microsoft.AspNetCore.Mvc;
using OAuthJWT.Api.Dtos;
using OAuthJWT.Api.Security;

namespace OAuthJWT.Api.Controllers;

[ApiController, Route("api/oauth")]
public sealed class AutenticacionController(TokenService tokenService) : ControllerBase
{
    [HttpPost("token")]
    public ActionResult<TokenResult> Authenticate(LoginRequest request)
    {
        var token = tokenService.Authenticate(request.Usuario, request.Contrasena);
        return token is null ? Unauthorized(new { message = "Usuario o contraseña incorrectos." }) : Ok(token);
    }
}
