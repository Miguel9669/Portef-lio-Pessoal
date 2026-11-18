namespace Portfolio.Core.Entidades;

// Datas preenchidas automaticamente pelo BackofficeDbContext ao gravar, sempre em UTC.

public interface ITemCriadoEm
{
    DateTime CriadoEm { get; set; }
}

public interface ITemDatas : ITemCriadoEm
{
    DateTime AtualizadoEm { get; set; }
}
