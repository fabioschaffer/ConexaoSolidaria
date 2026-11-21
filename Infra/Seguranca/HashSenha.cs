using Application.Usuarios;
using Microsoft.AspNetCore.Identity;

namespace Infra.Seguranca;

public sealed class HashSenha : IHashSenha
{
    private readonly PasswordHasher<object> _hasher = new();

    public string Gerar(string senha) => _hasher.HashPassword(this, senha);
}
