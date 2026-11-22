using Application.Doacoes;
using Application.Usuarios;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Authorize(Roles = nameof(PerfilUsuario.Doador))]
[Route("api/doacoes")]
public sealed class DoacoesController(DoacaoService doacoes, UsuarioService usuarios) : ControllerBase
{
    [HttpPost("doar")]
    public async Task<ActionResult<DoacaoRecebidaDto>> Registrar(
        RegistrarDoacaoDados dados, CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.Identity?.Name, out var doadorId) || doadorId <= 0)
        {
            return Forbid();
        }

        if (dados.IdCampanha <= 0)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, detail: "Informe um ID de campanha válido.");
        }

        try
        {
            var (resultado, doacao) = await doacoes.RegistrarAsync(doadorId, dados, cancellationToken);
            return resultado switch
            {
                ResultadoRegistroDoacao.Recebida => Accepted(doacao),
                ResultadoRegistroDoacao.PublicacaoNaoConfirmada => Problem(
                    statusCode: StatusCodes.Status503ServiceUnavailable,
                    detail: $"A doação {doacao!.Id} foi registrada, mas a publicação não foi confirmada."),
                ResultadoRegistroDoacao.DoadorInvalido => Forbid(),
                ResultadoRegistroDoacao.CampanhaNaoEncontrada => Problem(
                    statusCode: StatusCodes.Status404NotFound, detail: "Campanha não encontrada."),
                ResultadoRegistroDoacao.CampanhaIndisponivel => Problem(
                    statusCode: StatusCodes.Status409Conflict, detail: "A campanha não está recebendo doações."),
                _ => throw new ArgumentOutOfRangeException(nameof(resultado))
            };
        }
        catch (ArgumentOutOfRangeException ex) when (ex.ParamName == "valor")
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, detail: ex.Message);
        }
    }

    [HttpPost("criar-usuario")]
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
