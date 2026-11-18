using Portfolio.Core.Imagens;
using SkiaSharp;

namespace Portfolio.Infrastructure.Imagens;

// SEG17, SEG19, com o SkiaSharp (MIT).
// - O formato é detetado pelos primeiros bytes, nunca pela extensão nem pelo Content-Type.
//   SVG não é um formato que o SKCodec abra, por isso é sempre recusado.
// - A imagem é descodificada e codificada de novo a partir dos píxeis: um ficheiro malformado
//   não passa, e os metadados (EXIF com a localização GPS, IPTC, XMP) ficam todos de fora.
// - Antes disso aplica-se a orientação do EXIF, senão as fotos do telemóvel ficavam deitadas.
public sealed class ProcessadorSkia(OpcoesImagens opcoes) : IProcessadorImagens
{
    private const int Qualidade = 90;

    public ImagemProcessada? Processar(Stream conteudo)
    {
        using var copia = new MemoryStream();
        conteudo.CopyTo(copia);
        using var dados = SKData.CreateCopy(copia.ToArray());
        using var codec = SKCodec.Create(dados);
        if (codec is null || Formato(codec.EncodedFormat) is not { } formato)
            return null;

        // Uma imagem pequena em bytes pode ter dimensões enormes e ocupar gigabytes ao abrir.
        if ((long)codec.Info.Width * codec.Info.Height > opcoes.MaximoMegapixeis * 1_000_000L)
            return null;

        using var descodificada = SKBitmap.Decode(codec);
        if (descodificada is null)
            return null;

        using var orientada = Orientar(descodificada, codec.EncodedOrigin);
        using var imagem = SKImage.FromBitmap(orientada);
        using var codificada = imagem.Encode(formato.Tipo, Qualidade);
        return codificada is null ? null : new ImagemProcessada(codificada.ToArray(), formato.Extensao, formato.TipoMime);
    }

    private sealed record FormatoAceite(SKEncodedImageFormat Tipo, string Extensao, string TipoMime);

    private static FormatoAceite? Formato(SKEncodedImageFormat formato) => formato switch
    {
        SKEncodedImageFormat.Jpeg => new(SKEncodedImageFormat.Jpeg, ".jpg", "image/jpeg"),
        SKEncodedImageFormat.Png => new(SKEncodedImageFormat.Png, ".png", "image/png"),
        SKEncodedImageFormat.Webp => new(SKEncodedImageFormat.Webp, ".webp", "image/webp"),
        _ => null,
    };

    // Desenha a imagem já rodada ou espelhada, conforme a orientação guardada no EXIF.
    private static SKBitmap Orientar(SKBitmap origem, SKEncodedOrigin orientacao)
    {
        var (largura, altura) = (origem.Width, origem.Height);
        var trocaLados = orientacao is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop
            or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;

        var destino = new SKBitmap(new SKImageInfo(
            trocaLados ? altura : largura, trocaLados ? largura : altura, origem.ColorType, origem.AlphaType));
        using var tela = new SKCanvas(destino);
        tela.SetMatrix(Transformacao(orientacao, largura, altura));
        using var imagem = SKImage.FromBitmap(origem);
        tela.DrawImage(imagem, 0, 0, SKSamplingOptions.Default);
        return destino;
    }

    // x' = a·x + b·y + c ; y' = d·x + e·y + f
    private static SKMatrix Transformacao(SKEncodedOrigin orientacao, float l, float a) => orientacao switch
    {
        SKEncodedOrigin.TopRight => new SKMatrix(-1, 0, l, 0, 1, 0, 0, 0, 1),
        SKEncodedOrigin.BottomRight => new SKMatrix(-1, 0, l, 0, -1, a, 0, 0, 1),
        SKEncodedOrigin.BottomLeft => new SKMatrix(1, 0, 0, 0, -1, a, 0, 0, 1),
        SKEncodedOrigin.LeftTop => new SKMatrix(0, 1, 0, 1, 0, 0, 0, 0, 1),
        SKEncodedOrigin.RightTop => new SKMatrix(0, -1, a, 1, 0, 0, 0, 0, 1),
        SKEncodedOrigin.RightBottom => new SKMatrix(0, -1, a, -1, 0, l, 0, 0, 1),
        SKEncodedOrigin.LeftBottom => new SKMatrix(0, 1, 0, -1, 0, l, 0, 0, 1),
        _ => SKMatrix.Identity,
    };
}
