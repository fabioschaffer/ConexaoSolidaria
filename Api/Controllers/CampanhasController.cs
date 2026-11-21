using Application.Campanhas;
using Domain.Entidades;
using Domain.Enums;
using Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/campanhas")]
public sealed class CampanhasController(CampanhaService campanhas) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<CampanhaDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CampanhaDto>> Criar(
        CampanhaDados dados, CancellationToken cancellationToken)
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

    [HttpGet("{id:guid}")]
    [ProducesResponseType<CampanhaDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CampanhaDto>> Obter(Guid id, CancellationToken cancellationToken)
    {
        var campanha = await campanhas.ObterAsync(id, cancellationToken);
        return campanha is null ? CampanhaNaoEncontrada() : Ok(campanha);
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CampanhaDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<CampanhaDto>>> Listar(
        [FromQuery] StatusCampanha? status, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await campanhas.ListarAsync(status, cancellationToken));
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, detail: ex.Message);
        }
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Atualizar(
        Guid id, CampanhaDados dados, CancellationToken cancellationToken)
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

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancelar(Guid id, CancellationToken cancellationToken)
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
