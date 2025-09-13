using DesignacoesVidaMinisterio.Core.Interfaces;
using DesignacoesVidaMinisterio.Core.Models;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using System.Text;

namespace DesignacoesVidaMinisterio.Services;

/// <summary>
/// Serviço para extração de partes da reunião de apostilas em PDF
/// </summary>
public class PdfExtractionService : IPdfExtractionService
{
    public async Task<List<MeetingPart>> ExtractMeetingPartsAsync(string pdfFilePath, DateTime startDate)
    {
        var meetingParts = new List<MeetingPart>();

        if (!File.Exists(pdfFilePath))
        {
            throw new FileNotFoundException($"Arquivo PDF não encontrado: {pdfFilePath}");
        }

        try
        {
            using var pdfReader = new PdfReader(pdfFilePath);
            using var pdfDocument = new PdfDocument(pdfReader);

            var text = ExtractTextFromPdf(pdfDocument);
            meetingParts = ParseMeetingPartsFromText(text, startDate);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Erro ao processar o PDF: {ex.Message}", ex);
        }

        return meetingParts;
    }

    private string ExtractTextFromPdf(PdfDocument pdfDocument)
    {
        var text = new StringBuilder();
        
        for (int i = 1; i <= pdfDocument.GetNumberOfPages(); i++)
        {
            var page = pdfDocument.GetPage(i);
            var strategy = new SimpleTextExtractionStrategy();
            var pageText = PdfTextExtractor.GetTextFromPage(page, strategy);
            text.AppendLine(pageText);
        }

        return text.ToString();
    }

    private List<MeetingPart> ParseMeetingPartsFromText(string text, DateTime startDate)
    {
        var meetingParts = new List<MeetingPart>();
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // Esta é uma implementação básica - seria necessário ajustar conforme o formato real da apostila
        var currentDate = startDate;
        int partId = 1;

        // Padrões básicos para identificar partes da reunião
        var patterns = new Dictionary<string, TipoParticipacao>
        {
            { "LEITURA DA BÍBLIA", TipoParticipacao.Leitor },
            { "PRIMEIRA CONVERSA", TipoParticipacao.Estudante },
            { "SEGUNDA CONVERSA", TipoParticipacao.Estudante },
            { "TERCEIRA CONVERSA", TipoParticipacao.Estudante },
            { "PRIMEIRA PARTE", TipoParticipacao.Estudante },
            { "SEGUNDA PARTE", TipoParticipacao.Estudante },
            { "TERCEIRA PARTE", TipoParticipacao.Estudante },
            { "DEMONSTRAÇÃO", TipoParticipacao.Demonstracao },
            { "DISCURSO", TipoParticipacao.Discurso },
            { "PARTE DRAMÁTICA", TipoParticipacao.ParteDramatica },
            { "PRESIDENTE", TipoParticipacao.Presidente }
        };

        foreach (var line in lines)
        {
            var upperLine = line.ToUpper().Trim();
            
            // Verificar se é uma data de reunião
            if (TryParseDate(line, out DateTime reunionDate))
            {
                currentDate = reunionDate;
                continue;
            }

            // Procurar por padrões de partes da reunião
            foreach (var pattern in patterns)
            {
                if (upperLine.Contains(pattern.Key))
                {
                    var duration = ExtractDuration(line);
                    var title = CleanTitle(line);
                    var requiresHelper = DetermineIfRequiresHelper(pattern.Value, line);

                    var meetingPart = new MeetingPart
                    {
                        Id = partId++,
                        Titulo = title,
                        DuracaoMinutos = duration,
                        TipoParticipacao = pattern.Value,
                        DataReuniao = currentDate,
                        RequereAjudante = requiresHelper,
                        Descricao = ExtractDescription(line)
                    };

                    meetingParts.Add(meetingPart);
                    break;
                }
            }
        }

        // Se não encontrou partes específicas, criar partes básicas padrão
        if (!meetingParts.Any())
        {
            meetingParts = CreateDefaultMeetingParts(startDate);
        }

        return meetingParts;
    }

    private bool TryParseDate(string line, out DateTime date)
    {
        date = default;
        
        // Padrões comuns de data
        var datePatterns = new[]
        {
            @"\d{1,2}/\d{1,2}/\d{4}",
            @"\d{1,2}-\d{1,2}-\d{4}",
            @"\d{1,2} de \w+ de \d{4}"
        };

        foreach (var pattern in datePatterns)
        {
            var match = System.Text.RegularExpressions.Regex.Match(line, pattern);
            if (match.Success)
            {
                return DateTime.TryParse(match.Value, out date);
            }
        }

        return false;
    }

    private int ExtractDuration(string line)
    {
        // Procurar por padrões como "5 min", "(5 min)", "5 minutos"
        var match = System.Text.RegularExpressions.Regex.Match(line, @"(\d+)\s*(min|minuto|minutos)", 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        
        if (match.Success && int.TryParse(match.Groups[1].Value, out int minutes))
        {
            return minutes;
        }

        // Duração padrão se não encontrar
        return 5;
    }

    private string CleanTitle(string line)
    {
        // Remover números, durações e caracteres especiais para extrair o título limpo
        var title = line.Trim();
        title = System.Text.RegularExpressions.Regex.Replace(title, @"^\d+\.\s*", "");
        title = System.Text.RegularExpressions.Regex.Replace(title, @"\(\d+\s*min\w*\)", "", 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        title = title.Trim(' ', '-', ':', '.');
        
        return string.IsNullOrEmpty(title) ? "Parte da Reunião" : title;
    }

    private bool DetermineIfRequiresHelper(TipoParticipacao tipo, string line)
    {
        // Algumas partes tipicamente requerem ajudante
        switch (tipo)
        {
            case TipoParticipacao.Estudante:
            case TipoParticipacao.Demonstracao:
            case TipoParticipacao.ParteDramatica:
                return true;
            default:
                return false;
        }
    }

    private string? ExtractDescription(string line)
    {
        // Extrair descrição adicional se houver
        if (line.Contains(":"))
        {
            var parts = line.Split(':', 2);
            if (parts.Length > 1)
            {
                var description = parts[1].Trim();
                return string.IsNullOrEmpty(description) ? null : description;
            }
        }
        return null;
    }

    private List<MeetingPart> CreateDefaultMeetingParts(DateTime startDate)
    {
        // Criar estrutura básica padrão da reunião Vida e Ministério
        return new List<MeetingPart>
        {
            new MeetingPart
            {
                Id = 1,
                Titulo = "Abertura e Oração",
                DuracaoMinutos = 5,
                TipoParticipacao = TipoParticipacao.Presidente,
                DataReuniao = startDate,
                RequereAjudante = false
            },
            new MeetingPart
            {
                Id = 2,
                Titulo = "Leitura da Bíblia",
                DuracaoMinutos = 4,
                TipoParticipacao = TipoParticipacao.Leitor,
                DataReuniao = startDate,
                RequereAjudante = false
            },
            new MeetingPart
            {
                Id = 3,
                Titulo = "Primeira Conversa",
                DuracaoMinutos = 3,
                TipoParticipacao = TipoParticipacao.Estudante,
                DataReuniao = startDate,
                RequereAjudante = true
            },
            new MeetingPart
            {
                Id = 4,
                Titulo = "Segunda Conversa",
                DuracaoMinutos = 4,
                TipoParticipacao = TipoParticipacao.Estudante,
                DataReuniao = startDate,
                RequereAjudante = true
            },
            new MeetingPart
            {
                Id = 5,
                Titulo = "Terceira Conversa",
                DuracaoMinutos = 5,
                TipoParticipacao = TipoParticipacao.Estudante,
                DataReuniao = startDate,
                RequereAjudante = true
            }
        };
    }
}