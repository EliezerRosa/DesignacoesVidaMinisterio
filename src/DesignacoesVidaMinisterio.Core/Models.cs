namespace DesignacoesVidaMinisterio.Core.Models;

/// <summary>
/// Representa um publicador aprovado para receber designações
/// </summary>
public class Publisher
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Telefone { get; set; }
    public bool IsAprovado { get; set; } = true;
    public DateTime DataCadastro { get; set; } = DateTime.Now;
    public List<TipoParticipacao> TiposAprovados { get; set; } = new();
    public int TotalDesignacoes { get; set; } = 0;
    public DateTime? UltimaDesignacao { get; set; }
}

/// <summary>
/// Tipos de participação na reunião
/// </summary>
public enum TipoParticipacao
{
    Estudante,
    Ajudante,
    Presidente,
    Leitor,
    Discurso,
    Demonstracao,
    ParteDramatica
}

/// <summary>
/// Representa uma parte específica da reunião
/// </summary>
public class MeetingPart
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public int DuracaoMinutos { get; set; }
    public TipoParticipacao TipoParticipacao { get; set; }
    public DateTime DataReuniao { get; set; }
    public string? Descricao { get; set; }
    public bool RequereAjudante { get; set; } = false;
}

/// <summary>
/// Representa uma designação específica
/// </summary>
public class Assignment
{
    public int Id { get; set; }
    public int PublisherId { get; set; }
    public Publisher? Publisher { get; set; }
    public int MeetingPartId { get; set; }
    public MeetingPart? MeetingPart { get; set; }
    public int? AjudanteId { get; set; }
    public Publisher? Ajudante { get; set; }
    public DateTime DataDesignacao { get; set; } = DateTime.Now;
    public StatusDesignacao Status { get; set; } = StatusDesignacao.Ativa;
    public string? Observacoes { get; set; }
}

/// <summary>
/// Status da designação
/// </summary>
public enum StatusDesignacao
{
    Ativa,
    Concluida,
    Cancelada,
    Desistencia
}
