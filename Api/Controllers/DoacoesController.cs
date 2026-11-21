using Application.Doacoes;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Authorize(Roles = nameof(PerfilUsuario.Doador))]
[Route("api/doacoes")]
public sealed class DoacoesController(DoacaoService doacoes) : ControllerBase
{
    [HttpPost]
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
}
