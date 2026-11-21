namespace Domain.Exceptions;

public sealed class CampanhaCanceladaException()
    : InvalidOperationException("O status de uma campanha cancelada não pode ser alterado.")
{
}
