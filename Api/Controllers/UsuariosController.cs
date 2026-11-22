using Application.Usuarios;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Authorize(Roles = nameof(PerfilUsuario.GestorONG))]
[Route("api/usuarios")]
public sealed class UsuariosController(UsuarioService usuarios) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<UsuarioDto>> Criar(
        CriarUsuarioDados dados, CancellationToken cancellationToken)
    {
        try
        {
            var (resultado, usuario) = await usuarios.CriarAsync(dados, cancellationToken);
            return usuario is null
                ? ResponderAlteracao(resultado)
                : CreatedAtAction(nameof(Obter), new { id = usuario.Id }, usuario);
        }
        catch (ArgumentException ex)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, detail: ex.Message);
        }
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UsuarioDto>>> Listar(CancellationToken cancellationToken)
        => Ok(await usuarios.ListarAsync(cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UsuarioDto>> Obter(int id, CancellationToken cancellationToken)
    {
        var usuario = await usuarios.ObterAsync(id, cancellationToken);
        return usuario is null ? UsuarioNaoEncontrado() : Ok(usuario);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Atualizar(
        int id, AtualizarUsuarioDados dados, CancellationToken cancellationToken)
    {
        try
        {
            return ResponderAlteracao(await usuarios.AtualizarAsync(id, dados, cancellationToken));
        }
        catch (ArgumentException ex)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, detail: ex.Message);
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Remover(int id, CancellationToken cancellationToken)
        => ResponderAlteracao(await usuarios.RemoverAsync(id, cancellationToken));

    private ObjectResult UsuarioNaoEncontrado()
        => Problem(statusCode: StatusCodes.Status404NotFound, detail: "Usuário não encontrado.");

    private ActionResult ResponderAlteracao(ResultadoAlteracaoUsuario resultado) => resultado switch
    {
        ResultadoAlteracaoUsuario.Sucesso => NoContent(),
        ResultadoAlteracaoUsuario.NaoEncontrado => UsuarioNaoEncontrado(),
        ResultadoAlteracaoUsuario.PerfilInalteravel => Forbid(),
        ResultadoAlteracaoUsuario.EmailDuplicado => Problem(
            statusCode: StatusCodes.Status409Conflict, detail: "O e-mail já está cadastrado."),
        ResultadoAlteracaoUsuario.CpfDuplicado => Problem(
            statusCode: StatusCodes.Status409Conflict, detail: "O CPF já está cadastrado."),
        ResultadoAlteracaoUsuario.PossuiDoacoes => Problem(
            statusCode: StatusCodes.Status409Conflict, detail: "O usuário possui doações vinculadas."),
        ResultadoAlteracaoUsuario.Conflito => Problem(
            statusCode: StatusCodes.Status409Conflict, detail: "O usuário foi alterado por outra requisição."),
        _ => throw new ArgumentOutOfRangeException(nameof(resultado))
    };
}
