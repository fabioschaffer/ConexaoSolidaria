using Application.Campanhas;
using Domain.Enums;
using Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Authorize(Roles = nameof(PerfilUsuario.GestorONG))]
[Route("api/campanhas")]
public sealed class CampanhasController(CampanhaService campanhas) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<CampanhaDto>> Criar(
        CriarCampanhaDados dados, CancellationToken cancellationToken)
    {
        try
        {
            var campanha = await campanhas.CriarAsync(dados, cancellationToken);
            return CreatedAtAction(nameof(Obter), new { id = campanha.Id }, campanha);
        }
        catch (ArgumentException ex)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, detail: ex.Message);
        }
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CampanhaDto>> Obter(int id, CancellationToken cancellationToken)
    {
        var campanha = await campanhas.ObterAsync(id, cancellationToken);
        return campanha is null ? CampanhaNaoEncontrada() : Ok(campanha);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CampanhaDto>>> Listar(
        [FromQuery] int? id, CancellationToken cancellationToken)
        => Ok(await campanhas.ListarAsync(id, cancellationToken));

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Atualizar(
        int id, CampanhaDados dados, CancellationToken cancellationToken)
    {
        try
        {
            return ResponderAlteracao(await campanhas.AtualizarAsync(id, dados, cancellationToken));
        }
        catch (CampanhaCanceladaException ex)
        {
            return Problem(statusCode: StatusCodes.Status409Conflict, detail: ex.Message);
        }
        catch (ArgumentException ex)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, detail: ex.Message);
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Cancelar(int id, CancellationToken cancellationToken)
        => ResponderAlteracao(await campanhas.CancelarAsync(id, cancellationToken));

    private ObjectResult CampanhaNaoEncontrada()
        => Problem(statusCode: StatusCodes.Status404NotFound, detail: "Campanha não encontrada.");

    private IActionResult ResponderAlteracao(ResultadoAlteracaoCampanha resultado) => resultado switch
    {
        ResultadoAlteracaoCampanha.Sucesso => NoContent(),
        ResultadoAlteracaoCampanha.NaoEncontrada => CampanhaNaoEncontrada(),
        ResultadoAlteracaoCampanha.Conflito => Problem(
            statusCode: StatusCodes.Status409Conflict,
            detail: "A campanha foi alterada por outra requisição. Consulte o estado atual e tente novamente."),
        _ => throw new ArgumentOutOfRangeException(nameof(resultado))
    };
}
