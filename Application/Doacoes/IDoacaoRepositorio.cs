using Domain.Entidades;

namespace Application.Doacoes;

public interface IDoacaoRepositorio
{
    Task<Usuario?> ObterDoadorAsync(int id, CancellationToken cancellationToken);
    Task<Campanha?> ObterCampanhaAsync(int id, CancellationToken cancellationToken);
    Task SalvarAsync(Doacao doacao, CancellationToken cancellationToken);
}
