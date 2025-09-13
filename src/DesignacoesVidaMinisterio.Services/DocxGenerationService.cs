using DesignacoesVidaMinisterio.Core.Interfaces;
using DesignacoesVidaMinisterio.Core.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Globalization;

namespace DesignacoesVidaMinisterio.Services;

/// <summary>
/// Serviço para geração de documentos DOCX da programação semanal
/// </summary>
public class DocxGenerationService : IDocxGenerationService
{
    private readonly IDataService _dataService;

    public DocxGenerationService(IDataService dataService)
    {
        _dataService = dataService;
    }

    public async Task<string> GenerateWeeklyProgramAsync(List<Assignment> assignments, DateTime weekDate)
    {
        var outputDirectory = "output";
        if (!Directory.Exists(outputDirectory))
            Directory.CreateDirectory(outputDirectory);

        var fileName = $"Programacao_Semanal_{weekDate:yyyy_MM_dd}.docx";
        var filePath = Path.Combine(outputDirectory, fileName);

        // Buscar dados relacionados
        var meetingParts = await _dataService.GetMeetingPartsAsync(weekDate);
        var publishers = await _dataService.GetPublishersAsync();

        using var document = WordprocessingDocument.Create(filePath, WordprocessingDocumentType.Document);
        
        var mainPart = document.AddMainDocumentPart();
        mainPart.Document = new Document();
        var body = mainPart.Document.AppendChild(new Body());

        // Título do documento
        AddTitle(body, $"Programação da Reunião Vida e Ministério");
        AddSubtitle(body, $"Semana de {weekDate:dd/MM/yyyy}");

        // Adicionar linha em branco
        AddParagraph(body, "");

        // Agrupar assignments por data
        var assignmentsByDate = assignments
            .Join(meetingParts, a => a.MeetingPartId, mp => mp.Id, (a, mp) => new { Assignment = a, MeetingPart = mp })
            .GroupBy(x => x.MeetingPart.DataReuniao.Date)
            .OrderBy(g => g.Key);

        foreach (var dateGroup in assignmentsByDate)
        {
            AddSubheading(body, $"Reunião de {dateGroup.Key:dddd, dd/MM/yyyy}", true);

            var table = CreateAssignmentTable(body);
            
            foreach (var item in dateGroup.OrderBy(x => x.MeetingPart.Id))
            {
                var publisher = publishers.FirstOrDefault(p => p.Id == item.Assignment.PublisherId);
                var ajudante = item.Assignment.AjudanteId.HasValue 
                    ? publishers.FirstOrDefault(p => p.Id == item.Assignment.AjudanteId.Value)
                    : null;

                AddTableRow(table, 
                    item.MeetingPart.Titulo,
                    $"{item.MeetingPart.DuracaoMinutos} min",
                    publisher?.Nome ?? "Não designado",
                    ajudante?.Nome ?? "",
                    item.MeetingPart.TipoParticipacao.ToString());
            }

            AddParagraph(body, ""); // Linha em branco entre tabelas
        }

        // Adicionar observações gerais
        AddSubheading(body, "Observações", false);
        AddParagraph(body, "• Confirmar participação até terça-feira da semana anterior");
        AddParagraph(body, "• Em caso de impossibilidade, comunicar imediatamente");
        AddParagraph(body, "• Preparar o material com antecedência");

        document.Save();
        return filePath;
    }

    private void AddTitle(Body body, string text)
    {
        var paragraph = body.AppendChild(new Paragraph());
        var run = paragraph.AppendChild(new Run());
        var runProperties = run.AppendChild(new RunProperties());
        runProperties.Bold = new Bold();
        runProperties.FontSize = new FontSize() { Val = "28" };
        run.AppendChild(new Text(text));
        
        var paragraphProperties = paragraph.AppendChild(new ParagraphProperties());
        paragraphProperties.Justification = new Justification() { Val = JustificationValues.Center };
    }

    private void AddSubtitle(Body body, string text)
    {
        var paragraph = body.AppendChild(new Paragraph());
        var run = paragraph.AppendChild(new Run());
        var runProperties = run.AppendChild(new RunProperties());
        runProperties.Bold = new Bold();
        runProperties.FontSize = new FontSize() { Val = "16" };
        run.AppendChild(new Text(text));
        
        var paragraphProperties = paragraph.AppendChild(new ParagraphProperties());
        paragraphProperties.Justification = new Justification() { Val = JustificationValues.Center };
    }

    private void AddSubheading(Body body, string text, bool bold = true)
    {
        var paragraph = body.AppendChild(new Paragraph());
        var run = paragraph.AppendChild(new Run());
        var runProperties = run.AppendChild(new RunProperties());
        if (bold) runProperties.Bold = new Bold();
        runProperties.FontSize = new FontSize() { Val = "14" };
        run.AppendChild(new Text(text));
    }

    private void AddParagraph(Body body, string text)
    {
        var paragraph = body.AppendChild(new Paragraph());
        var run = paragraph.AppendChild(new Run());
        run.AppendChild(new Text(text));
    }

    private Table CreateAssignmentTable(Body body)
    {
        var table = body.AppendChild(new Table());

        // Propriedades da tabela
        var tableProperties = table.AppendChild(new TableProperties());
        var tableBorders = tableProperties.AppendChild(new TableBorders());
        
        tableBorders.TopBorder = new TopBorder() { Val = BorderValues.Single, Size = 4 };
        tableBorders.BottomBorder = new BottomBorder() { Val = BorderValues.Single, Size = 4 };
        tableBorders.LeftBorder = new LeftBorder() { Val = BorderValues.Single, Size = 4 };
        tableBorders.RightBorder = new RightBorder() { Val = BorderValues.Single, Size = 4 };
        tableBorders.InsideHorizontalBorder = new InsideHorizontalBorder() { Val = BorderValues.Single, Size = 4 };
        tableBorders.InsideVerticalBorder = new InsideVerticalBorder() { Val = BorderValues.Single, Size = 4 };

        // Cabeçalho da tabela
        var headerRow = table.AppendChild(new TableRow());
        AddTableCell(headerRow, "Parte", true);
        AddTableCell(headerRow, "Duração", true);
        AddTableCell(headerRow, "Participante", true);
        AddTableCell(headerRow, "Ajudante", true);
        AddTableCell(headerRow, "Tipo", true);

        return table;
    }

    private void AddTableRow(Table table, string parte, string duracao, string participante, string ajudante, string tipo)
    {
        var row = table.AppendChild(new TableRow());
        AddTableCell(row, parte);
        AddTableCell(row, duracao);
        AddTableCell(row, participante);
        AddTableCell(row, ajudante);
        AddTableCell(row, tipo);
    }

    private void AddTableCell(TableRow row, string text, bool isHeader = false)
    {
        var cell = row.AppendChild(new TableCell());
        var paragraph = cell.AppendChild(new Paragraph());
        var run = paragraph.AppendChild(new Run());
        
        if (isHeader)
        {
            var runProperties = run.AppendChild(new RunProperties());
            runProperties.Bold = new Bold();
        }
        
        run.AppendChild(new Text(text));
        
        // Propriedades da célula
        var cellProperties = cell.AppendChild(new TableCellProperties());
        cellProperties.TableCellWidth = new TableCellWidth() { Type = TableWidthUnitValues.Auto };
    }
}