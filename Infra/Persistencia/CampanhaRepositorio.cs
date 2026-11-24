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
        int? id, CancellationToken cancellationToken)
    {
        var consulta = contexto.Campanhas.AsNoTracking();
        if (id.HasValue)
        {
            consulta = consulta.Where(c => c.Id == id.Value);
        }

        return await consulta.OrderByDescending(c => c.DataInicio)
            .ThenBy(c => c.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CampanhaTransparenciaDto>> ListarParaTransparenciaAsync(
        int? id, CancellationToken cancellationToken)
    {
        var consulta = contexto.Campanhas.AsNoTracking()
            .Where(c => c.Status == StatusCampanha.Ativa);
        if (id.HasValue)
        {
            consulta = consulta.Where(c => c.Id == id.Value);
        }

        return await consulta.OrderByDescending(c => c.DataInicio)
            .ThenBy(c => c.Id)
            .Select(c => new CampanhaTransparenciaDto(
                c.Id,
                c.Titulo,
                c.MetaFinanceira,
                c.ValorTotalArrecadado,
                c.Status))
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
