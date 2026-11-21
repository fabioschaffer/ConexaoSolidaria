using System.ComponentModel.DataAnnotations;
using Api.Seguranca;
using Application.Usuarios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/autenticacao")]
public sealed class AutenticacaoController(UsuarioService usuarios, TokenJwt tokens) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<ActionResult<RespostaToken>> Login(LoginDados dados, CancellationToken cancellationToken)
    {
        var usuario = await usuarios.AutenticarAsync(dados.Email, dados.Senha, cancellationToken);
        return usuario is null
            ? Problem(statusCode: StatusCodes.Status401Unauthorized, detail: "E-mail ou senha inválidos.")
            : Ok(tokens.Emitir(usuario));
    }
}

public sealed record LoginDados([Required] string Email, [Required] string Senha);
