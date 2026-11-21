using Application.Usuarios;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/usuarios")]
public sealed class UsuariosController(UsuarioService usuarios) : ControllerBase
{
    [HttpPost]
    [AllowAnonymous]
    [ProducesResponseType<UsuarioDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UsuarioDto>> Criar(
        CriarUsuarioDados dados, CancellationToken cancellationToken)
    {
        if (dados.Perfil != PerfilUsuario.Doador)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest,
                detail: "O cadastro público está disponível para doadores.");
        }

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
    [Authorize(Roles = nameof(PerfilUsuario.GestorONG))]
    public async Task<ActionResult<IReadOnlyList<UsuarioDto>>> Listar(CancellationToken cancellationToken)
        => Ok(await usuarios.ListarAsync(cancellationToken));

    [HttpGet("{id:int}")]
    [ProducesResponseType<UsuarioDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UsuarioDto>> Obter(int id, CancellationToken cancellationToken)
    {
        if (!EhTitular(id))
        {
            return Forbid();
        }

        var usuario = await usuarios.ObterAsync(id, cancellationToken);
        return usuario is null ? UsuarioNaoEncontrado() : Ok(usuario);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Atualizar(
        int id, AtualizarUsuarioDados dados, CancellationToken cancellationToken)
    {
        if (!EhTitular(id))
        {
            return Forbid();
        }

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
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Remover(int id, CancellationToken cancellationToken)
        => EhTitular(id) ? ResponderAlteracao(await usuarios.RemoverAsync(id, cancellationToken)) : Forbid();

    private bool EhTitular(int id) => int.TryParse(User.Identity?.Name, out var idToken) && idToken == id;

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
