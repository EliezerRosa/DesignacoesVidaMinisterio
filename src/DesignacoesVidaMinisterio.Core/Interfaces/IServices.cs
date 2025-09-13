using DesignacoesVidaMinisterio.Core.Models;

namespace DesignacoesVidaMinisterio.Core.Interfaces;

/// <summary>
/// Interface para extração de dados de apostilas em PDF
/// </summary>
public interface IPdfExtractionService
{
    Task<List<MeetingPart>> ExtractMeetingPartsAsync(string pdfFilePath, DateTime startDate);
}

/// <summary>
/// Interface para geração de documentos DOCX
/// </summary>
public interface IDocxGenerationService
{
    Task<string> GenerateWeeklyProgramAsync(List<Assignment> assignments, DateTime weekDate);
}

/// <summary>
/// Interface para preenchimento de formulários PDF S-89
/// </summary>
public interface IPdfFormService
{
    Task<string> FillS89FormAsync(Assignment assignment);
}

/// <summary>
/// Interface para notificações via WhatsApp Web
/// </summary>
public interface IWhatsAppService
{
    Task SendNotificationAsync(string phoneNumber, string message);
    Task SendAssignmentNotificationAsync(Assignment assignment);
}

/// <summary>
/// Interface para persistência de dados em planilhas
/// </summary>
public interface IDataService
{
    Task<List<Publisher>> GetPublishersAsync();
    Task<Publisher> SavePublisherAsync(Publisher publisher);
    Task<List<Assignment>> GetAssignmentsAsync(DateTime? startDate = null, DateTime? endDate = null);
    Task<Assignment> SaveAssignmentAsync(Assignment assignment);
    Task<List<MeetingPart>> GetMeetingPartsAsync(DateTime? date = null);
    Task<MeetingPart> SaveMeetingPartAsync(MeetingPart meetingPart);
}

/// <summary>
/// Interface para algoritmo de designações
/// </summary>
public interface IAssignmentService
{
    Task<List<Assignment>> GenerateWeeklyAssignmentsAsync(DateTime weekDate);
    Task<Assignment> AssignPublisherToPartAsync(int meetingPartId, int publisherId, int? ajudanteId = null);
    Task<bool> ValidateAssignmentAsync(Assignment assignment);
    Task RegisterDropoutAsync(int assignmentId, string reason);
}