using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Application.Usuarios;
using Microsoft.IdentityModel.Tokens;

namespace Api.Seguranca;

public sealed class TokenJwt(IConfiguration configuration, TimeProvider relogio)
{
    private readonly string _emissor = configuration["Jwt:Emissor"] ?? "ConexaoSolidaria";
    private readonly string _audiencia = configuration["Jwt:Audiencia"] ?? "ConexaoSolidaria.Api";
    private readonly SymmetricSecurityKey _chave = LerChave(configuration);

    public TokenValidationParameters ParametrosValidacao => new()
    {
        ValidIssuer = _emissor,
        ValidAudience = _audiencia,
        IssuerSigningKey = _chave,
        ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,
        NameClaimType = JwtRegisteredClaimNames.Sub,
        RoleClaimType = "role"
    };

    public RespostaToken Emitir(UsuarioDto usuario)
    {
        var validade = TimeSpan.FromHours(8);
        var agora = relogio.GetUtcNow();
        var expiraEm = agora.Add(validade);
        var token = new JwtSecurityToken(
            _emissor,
            _audiencia,
            [
                new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString(CultureInfo.InvariantCulture)),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
                new Claim(JwtRegisteredClaimNames.Iat,
                    agora.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture), ClaimValueTypes.Integer64),
                new Claim("role", usuario.Perfil.ToString())
            ],
            agora.UtcDateTime,
            expiraEm.UtcDateTime,
            new SigningCredentials(_chave, SecurityAlgorithms.HmacSha256));

        return new RespostaToken(new JwtSecurityTokenHandler().WriteToken(token), "Bearer", (int)validade.TotalSeconds);
    }

    private static SymmetricSecurityKey LerChave(IConfiguration configuration)
    {
        var valor = configuration["Jwt:Chave"];
        Span<byte> chave = new byte[valor?.Length ?? 0];
        if (valor is null || !Convert.TryFromBase64String(valor, chave, out var tamanho) || tamanho < 32)
        {
            throw new InvalidOperationException("Configure Jwt:Chave com pelo menos 32 bytes aleatórios em Base64.");
        }

        return new SymmetricSecurityKey(chave[..tamanho].ToArray());
    }
}

public sealed record RespostaToken(string AccessToken, string TokenType, int ExpiresIn);
