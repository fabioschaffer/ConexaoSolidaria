using Application.Usuarios;
using Microsoft.AspNetCore.Identity;

namespace Infra.Seguranca;

public sealed class HashSenha : IHashSenha
{
    private readonly PasswordHasher<object> _hasher = new();

    public string Gerar(string senha) => _hasher.HashPassword(this, senha);

    public bool Verificar(string hash, string senha)
    {
        try
        {
            return _hasher.VerifyHashedPassword(this, hash, senha) != PasswordVerificationResult.Failed;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
