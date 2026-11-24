using Domain.Entidades;

namespace Application.Campanhas;

public interface ICampanhaRepositorio
{
    Task AdicionarAsync(Campanha campanha, CancellationToken cancellationToken);
    Task<Campanha?> ObterAsync(int id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Campanha>> ListarAsync(int? id, CancellationToken cancellationToken);
    Task<IReadOnlyList<CampanhaTransparenciaDto>> ListarParaTransparenciaAsync(
        int? id, CancellationToken cancellationToken);
    Task<bool> TentarSalvarAlteracoesAsync(CancellationToken cancellationToken);
}
