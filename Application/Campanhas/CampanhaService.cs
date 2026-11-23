using Domain.Entidades;
using Domain.Enums;

namespace Application.Campanhas;

public sealed class CampanhaService(ICampanhaRepositorio repositorio, TimeProvider relogio)
{
    public async Task<CampanhaDto> CriarAsync(CriarCampanhaDados dados, CancellationToken cancellationToken)
    {
        var campanha = Campanha.Criar(dados.Titulo, dados.Descricao, dados.DataInicio,
            dados.DataFim, dados.MetaFinanceira, StatusCampanha.Ativa, relogio.GetUtcNow());
        await repositorio.AdicionarAsync(campanha, cancellationToken);
        return CampanhaDto.De(campanha);
    }

    public async Task<CampanhaDto?> ObterAsync(int id, CancellationToken cancellationToken)
    {
        var campanha = await repositorio.ObterAsync(id, cancellationToken);
        return campanha is null ? null : CampanhaDto.De(campanha);
    }

    public async Task<IReadOnlyList<CampanhaDto>> ListarAsync(
        StatusCampanha? status, CancellationToken cancellationToken)
    {
        if (status.HasValue && !Enum.IsDefined(status.Value))
        {
            throw new ArgumentOutOfRangeException(nameof(status), "Status de campanha inválido.");
        }

        var campanhas = await repositorio.ListarAsync(status, cancellationToken);
        return campanhas.Select(CampanhaDto.De).ToArray();
    }

    public Task<IReadOnlyList<CampanhaTransparenciaDto>> ListarParaTransparenciaAsync(
        CancellationToken cancellationToken)
        => repositorio.ListarParaTransparenciaAsync(cancellationToken);

    public async Task<ResultadoAlteracaoCampanha> AtualizarAsync(
        int id, CampanhaDados dados, CancellationToken cancellationToken)
    {
        var campanha = await repositorio.ObterAsync(id, cancellationToken);
        if (campanha is null)
        {
            return ResultadoAlteracaoCampanha.NaoEncontrada;
        }

        campanha.Editar(dados.Titulo, dados.Descricao, dados.DataInicio,
            dados.DataFim, dados.MetaFinanceira, dados.Status);
        return await SalvarAsync(cancellationToken);
    }

    public async Task<ResultadoAlteracaoCampanha> CancelarAsync(int id, CancellationToken cancellationToken)
    {
        var campanha = await repositorio.ObterAsync(id, cancellationToken);
        if (campanha is null)
        {
            return ResultadoAlteracaoCampanha.NaoEncontrada;
        }

        campanha.Cancelar();
        return await SalvarAsync(cancellationToken);
    }

    private async Task<ResultadoAlteracaoCampanha> SalvarAsync(CancellationToken cancellationToken)
        => await repositorio.TentarSalvarAlteracoesAsync(cancellationToken)
            ? ResultadoAlteracaoCampanha.Sucesso
            : ResultadoAlteracaoCampanha.Conflito;
}

public enum ResultadoAlteracaoCampanha
{
    Sucesso,
    NaoEncontrada,
    Conflito
}
