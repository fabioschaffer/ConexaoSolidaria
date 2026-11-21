using Application.Doacoes;
using Domain.Entidades;

namespace Infra.Persistencia;

public sealed class DoacaoRepositorio(ConexaoSolidariaContext contexto) : IDoacaoRepositorio
{
    public async Task<Usuario?> ObterDoadorAsync(int id, CancellationToken cancellationToken)
        => await contexto.Usuarios.FindAsync([id], cancellationToken);

    public async Task<Campanha?> ObterCampanhaAsync(int id, CancellationToken cancellationToken)
        => await contexto.Campanhas.FindAsync([id], cancellationToken);

    public async Task SalvarAsync(Doacao doacao, CancellationToken cancellationToken)
    {
        contexto.Doacoes.Add(doacao);
        await contexto.SaveChangesAsync(cancellationToken);
    }
}
