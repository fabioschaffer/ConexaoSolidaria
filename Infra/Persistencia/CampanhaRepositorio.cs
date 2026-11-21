using Application.Campanhas;
using Domain.Entidades;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infra.Persistencia;

public sealed class CampanhaRepositorio(ConexaoSolidariaContext contexto) : ICampanhaRepositorio
{
    public async Task AdicionarAsync(Campanha campanha, CancellationToken cancellationToken)
    {
        contexto.Campanhas.Add(campanha);
        await contexto.SaveChangesAsync(cancellationToken);
    }

    public Task<Campanha?> ObterAsync(int id, CancellationToken cancellationToken)
        => contexto.Campanhas.SingleOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Campanha>> ListarAsync(
        StatusCampanha? status, CancellationToken cancellationToken)
    {
        var consulta = contexto.Campanhas.AsNoTracking();
        if (status.HasValue)
        {
            consulta = consulta.Where(c => c.Status == status.Value);
        }

        return await consulta.OrderByDescending(c => c.DataInicio)
            .ThenBy(c => c.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> TentarSalvarAlteracoesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await contexto.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
    }
}
