using System.ComponentModel.DataAnnotations;

namespace OAuthJWT.Api.Dtos;

public sealed record LoginRequest([Required] string Usuario, [Required] string Contrasena);
