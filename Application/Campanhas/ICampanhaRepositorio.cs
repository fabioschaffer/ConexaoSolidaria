using Domain.Entidades;
using Domain.Enums;

namespace Application.Campanhas;

public interface ICampanhaRepositorio
{
    Task AdicionarAsync(Campanha campanha, CancellationToken cancellationToken);
    Task<Campanha?> ObterAsync(int id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Campanha>> ListarAsync(StatusCampanha? status, CancellationToken cancellationToken);
    Task<bool> TentarSalvarAlteracoesAsync(CancellationToken cancellationToken);
}
