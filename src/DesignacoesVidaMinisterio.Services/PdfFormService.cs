using DesignacoesVidaMinisterio.Core.Interfaces;
using DesignacoesVidaMinisterio.Core.Models;
using iText.Forms;
using iText.Forms.Fields;
using iText.Kernel.Pdf;

namespace DesignacoesVidaMinisterio.Services;

/// <summary>
/// Serviço para preenchimento de formulários PDF S-89
/// </summary>
public class PdfFormService : IPdfFormService
{
    public async Task<string> FillS89FormAsync(Assignment assignment)
    {
        if (assignment.Publisher == null || assignment.MeetingPart == null)
        {
            throw new ArgumentException("Assignment deve ter Publisher e MeetingPart carregados");
        }

        var outputDirectory = "output";
        if (!Directory.Exists(outputDirectory))
            Directory.CreateDirectory(outputDirectory);

        var outputFileName = $"S89_{assignment.Publisher.Nome}_{assignment.MeetingPart.DataReuniao:yyyy_MM_dd}.pdf";
        var outputPath = Path.Combine(outputDirectory, outputFileName);

        // Para demonstração, vamos criar um PDF simples
        // Em uma implementação real, você usaria um template S-89 existente
        await CreateS89FormAsync(assignment, outputPath);

        return outputPath;
    }

    private async Task CreateS89FormAsync(Assignment assignment, string outputPath)
    {
        using var writer = new PdfWriter(outputPath);
        using var pdf = new PdfDocument(writer);
        var document = new iText.Layout.Document(pdf);

        // Adicionar conteúdo do formulário S-89
        document.Add(new iText.Layout.Element.Paragraph("FORMULÁRIO S-89")
            .SetFontSize(16)
            .SetTextAlignment(iText.Layout.Properties.TextAlignment.CENTER));

        document.Add(new iText.Layout.Element.Paragraph($"Nome: {assignment.Publisher?.Nome}"));
        document.Add(new iText.Layout.Element.Paragraph($"Email: {assignment.Publisher?.Email}"));
        document.Add(new iText.Layout.Element.Paragraph($"Telefone: {assignment.Publisher?.Telefone ?? "N/A"}"));
        
        document.Add(new iText.Layout.Element.Paragraph($"Parte Designada: {assignment.MeetingPart?.Titulo}"));
        document.Add(new iText.Layout.Element.Paragraph($"Data da Reunião: {assignment.MeetingPart?.DataReuniao:dd/MM/yyyy}"));
        document.Add(new iText.Layout.Element.Paragraph($"Duração: {assignment.MeetingPart?.DuracaoMinutos} minutos"));
        document.Add(new iText.Layout.Element.Paragraph($"Tipo: {assignment.MeetingPart?.TipoParticipacao}"));

        if (assignment.AjudanteId.HasValue && assignment.Ajudante != null)
        {
            document.Add(new iText.Layout.Element.Paragraph($"Ajudante: {assignment.Ajudante.Nome}"));
        }

        document.Add(new iText.Layout.Element.Paragraph($"Data da Designação: {assignment.DataDesignacao:dd/MM/yyyy}"));
        
        if (!string.IsNullOrEmpty(assignment.Observacoes))
        {
            document.Add(new iText.Layout.Element.Paragraph($"Observações: {assignment.Observacoes}"));
        }

        // Adicionar campos para preenchimento manual
        document.Add(new iText.Layout.Element.Paragraph("\n\nCampos para preenchimento:"));
        document.Add(new iText.Layout.Element.Paragraph("Texto Bíblico: ________________________________"));
        document.Add(new iText.Layout.Element.Paragraph("Tema: ________________________________________"));
        document.Add(new iText.Layout.Element.Paragraph("Cenário: _____________________________________"));
        document.Add(new iText.Layout.Element.Paragraph("Objetivo Principal: ____________________________"));

        document.Add(new iText.Layout.Element.Paragraph("\n\nAssinatura do Estudante: _______________________"));
        document.Add(new iText.Layout.Element.Paragraph("Data: _______________"));

        document.Close();
    }
}