using Application.Usuarios;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/doadores")]
public sealed class DoadorController(UsuarioService usuarios) : ControllerBase
{
    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<UsuarioDto>> Cadastrar(
        CadastrarDoadorDados dados, CancellationToken cancellationToken)
    {
        var novoUsuario = new CriarUsuarioDados(
            dados.Nome, dados.Email, dados.Cpf, PerfilUsuario.Doador, dados.Senha);

        try
        {
            var (resultado, usuario) = await usuarios.CriarAsync(novoUsuario, cancellationToken);
            return resultado switch
            {
                ResultadoAlteracaoUsuario.Sucesso => StatusCode(StatusCodes.Status201Created, usuario),
                ResultadoAlteracaoUsuario.EmailDuplicado => Problem(
                    statusCode: StatusCodes.Status409Conflict, detail: "O e-mail já está cadastrado."),
                ResultadoAlteracaoUsuario.CpfDuplicado => Problem(
                    statusCode: StatusCodes.Status409Conflict, detail: "O CPF já está cadastrado."),
                ResultadoAlteracaoUsuario.Conflito => Problem(
                    statusCode: StatusCodes.Status409Conflict, detail: "Conflito ao cadastrar o doador."),
                _ => throw new ArgumentOutOfRangeException(nameof(resultado))
            };
        }
        catch (ArgumentException ex)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, detail: ex.Message);
        }
    }
}

public sealed record CadastrarDoadorDados(string Nome, string Email, string Cpf, string Senha);
