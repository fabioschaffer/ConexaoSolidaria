namespace Domain.Entidades;

public sealed class CampanhaCanceladaException()
    : InvalidOperationException("O status de uma campanha cancelada não pode ser alterado.")
{
}
