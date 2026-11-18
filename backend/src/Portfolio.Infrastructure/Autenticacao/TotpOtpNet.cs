using OtpNet;
using Portfolio.Core.Autenticacao;
using QRCoder;

namespace Portfolio.Infrastructure.Autenticacao;

// TOTP compatível com Google Authenticator, Authy e Microsoft Authenticator (SEG09):
// códigos de 6 dígitos, intervalos de 30 s, SHA-1.
public sealed class TotpOtpNet : ITotp
{
    private const string Emissor = "Portfolio";

    // 160 bits, o tamanho recomendado pelo RFC 4226.
    public string GerarSegredo() => Base32Encoding.ToString(KeyGeneration.GenerateRandomKey(20));

    public ConfiguracaoTotp Configuracao(string segredo, string conta)
    {
        var uri = new OtpUri(OtpType.Totp, segredo, conta, Emissor).ToString();

        using var gerador = new QRCodeGenerator();
        using var dados = gerador.CreateQrCode(uri, QRCodeGenerator.ECCLevel.M);
        var png = new PngByteQRCode(dados).GetGraphic(6);

        return new ConfiguracaoTotp(segredo, uri, "data:image/png;base64," + Convert.ToBase64String(png));
    }

    // Aceita o intervalo atual e o anterior e o seguinte, para tolerar relógios ligeiramente desacertados.
    public long? Validar(string segredo, string codigo, DateTimeOffset agora)
    {
        if (codigo.Length != 6 || !codigo.All(char.IsAsciiDigit))
            return null;

        var totp = new Totp(Base32Encoding.ToBytes(segredo));
        return totp.VerifyTotp(agora.UtcDateTime, codigo, out var passo, VerificationWindow.RfcSpecifiedNetworkDelay)
            ? passo
            : null;
    }
}
