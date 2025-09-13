using DesignacoesVidaMinisterio.Core.Interfaces;
using DesignacoesVidaMinisterio.Core.Models;

namespace DesignacoesVidaMinisterio.Services;

/// <summary>
/// Implementação do serviço de designações com algoritmo de rodízio justo
/// </summary>
public class AssignmentService : IAssignmentService
{
    private readonly IDataService _dataService;

    public AssignmentService(IDataService dataService)
    {
        _dataService = dataService;
    }

    public async Task<List<Assignment>> GenerateWeeklyAssignmentsAsync(DateTime weekDate)
    {
        var assignments = new List<Assignment>();
        var meetingParts = await _dataService.GetMeetingPartsAsync(weekDate);
        var publishers = await _dataService.GetPublishersAsync();
        var activePublishers = publishers.Where(p => p.IsAprovado).ToList();

        foreach (var part in meetingParts)
        {
            var assignment = await AssignBestPublisherForPartAsync(part, activePublishers);
            if (assignment != null)
            {
                assignments.Add(assignment);
                
                // Atualizar estatísticas do publisher
                var publisher = activePublishers.First(p => p.Id == assignment.PublisherId);
                publisher.TotalDesignacoes++;
                publisher.UltimaDesignacao = weekDate;
                await _dataService.SavePublisherAsync(publisher);

                // Se requer ajudante, designar também
                if (part.RequereAjudante && assignment.AjudanteId.HasValue)
                {
                    var ajudante = activePublishers.First(p => p.Id == assignment.AjudanteId.Value);
                    ajudante.TotalDesignacoes++;
                    ajudante.UltimaDesignacao = weekDate;
                    await _dataService.SavePublisherAsync(ajudante);
                }
            }
        }

        return assignments;
    }

    public async Task<Assignment> AssignPublisherToPartAsync(int meetingPartId, int publisherId, int? ajudanteId = null)
    {
        var meetingPart = (await _dataService.GetMeetingPartsAsync()).FirstOrDefault(mp => mp.Id == meetingPartId);
        var publisher = (await _dataService.GetPublishersAsync()).FirstOrDefault(p => p.Id == publisherId);

        if (meetingPart == null || publisher == null)
            throw new ArgumentException("MeetingPart ou Publisher não encontrado.");

        var assignment = new Assignment
        {
            PublisherId = publisherId,
            MeetingPartId = meetingPartId,
            AjudanteId = ajudanteId,
            DataDesignacao = DateTime.Now,
            Status = StatusDesignacao.Ativa
        };

        if (!await ValidateAssignmentAsync(assignment))
            throw new InvalidOperationException("Designação inválida.");

        return await _dataService.SaveAssignmentAsync(assignment);
    }

    public async Task<bool> ValidateAssignmentAsync(Assignment assignment)
    {
        var meetingPart = (await _dataService.GetMeetingPartsAsync()).FirstOrDefault(mp => mp.Id == assignment.MeetingPartId);
        var publisher = (await _dataService.GetPublishersAsync()).FirstOrDefault(p => p.Id == assignment.PublisherId);

        if (meetingPart == null || publisher == null)
            return false;

        // Verificar se o publisher está aprovado
        if (!publisher.IsAprovado)
            return false;

        // Verificar se o publisher tem aprovação para este tipo de participação
        if (!publisher.TiposAprovados.Contains(meetingPart.TipoParticipacao))
            return false;

        // Verificar se não há conflito de data (publisher já designado na mesma data)
        var existingAssignments = await _dataService.GetAssignmentsAsync(
            meetingPart.DataReuniao.Date, 
            meetingPart.DataReuniao.Date.AddDays(1).AddTicks(-1));

        var hasConflict = existingAssignments.Any(a => 
            a.PublisherId == assignment.PublisherId && 
            a.Status == StatusDesignacao.Ativa &&
            a.Id != assignment.Id);

        return !hasConflict;
    }

    public async Task RegisterDropoutAsync(int assignmentId, string reason)
    {
        var assignments = await _dataService.GetAssignmentsAsync();
        var assignment = assignments.FirstOrDefault(a => a.Id == assignmentId);

        if (assignment == null)
            throw new ArgumentException("Designação não encontrada.");

        assignment.Status = StatusDesignacao.Desistencia;
        assignment.Observacoes = string.IsNullOrEmpty(assignment.Observacoes) 
            ? $"Desistência: {reason}" 
            : $"{assignment.Observacoes}; Desistência: {reason}";

        await _dataService.SaveAssignmentAsync(assignment);
    }

    private async Task<Assignment?> AssignBestPublisherForPartAsync(MeetingPart part, List<Publisher> activePublishers)
    {
        // Filtrar publishers aprovados para este tipo de participação
        var eligiblePublishers = activePublishers
            .Where(p => p.TiposAprovados.Contains(part.TipoParticipacao))
            .ToList();

        if (!eligiblePublishers.Any())
            return null;

        // Algoritmo de rodízio justo: priorizar por:
        // 1. Menor número total de designações
        // 2. Designação mais antiga (ou nunca designado)
        // 3. Não ter sido designado recentemente na mesma semana

        var recentAssignments = await _dataService.GetAssignmentsAsync(
            part.DataReuniao.AddDays(-7), 
            part.DataReuniao.AddDays(7));

        var recentlyAssignedPublisherIds = recentAssignments
            .Where(a => a.Status == StatusDesignacao.Ativa)
            .Select(a => a.PublisherId)
            .Distinct()
            .ToHashSet();

        // Calcular score para cada publisher elegível
        var publisherScores = eligiblePublishers.Select(p => new
        {
            Publisher = p,
            Score = CalculateAssignmentScore(p, recentlyAssignedPublisherIds.Contains(p.Id), part.DataReuniao)
        }).OrderBy(ps => ps.Score).ToList();

        var bestPublisher = publisherScores.First().Publisher;

        var assignment = new Assignment
        {
            PublisherId = bestPublisher.Id,
            MeetingPartId = part.Id,
            DataDesignacao = DateTime.Now,
            Status = StatusDesignacao.Ativa
        };

        // Se requer ajudante, encontrar o melhor ajudante
        if (part.RequereAjudante)
        {
            var eligibleAjudantes = eligiblePublishers
                .Where(p => p.Id != bestPublisher.Id)
                .ToList();

            if (eligibleAjudantes.Any())
            {
                var ajudanteScores = eligibleAjudantes.Select(p => new
                {
                    Publisher = p,
                    Score = CalculateAssignmentScore(p, recentlyAssignedPublisherIds.Contains(p.Id), part.DataReuniao)
                }).OrderBy(ps => ps.Score).ToList();

                assignment.AjudanteId = ajudanteScores.First().Publisher.Id;
            }
        }

        return await _dataService.SaveAssignmentAsync(assignment);
    }

    private double CalculateAssignmentScore(Publisher publisher, bool recentlyAssigned, DateTime meetingDate)
    {
        double score = 0;

        // Peso para total de designações (menor é melhor)
        score += publisher.TotalDesignacoes * 10;

        // Peso para tempo desde última designação (mais tempo é melhor)
        if (publisher.UltimaDesignacao.HasValue)
        {
            var daysSinceLastAssignment = (meetingDate - publisher.UltimaDesignacao.Value).TotalDays;
            score -= daysSinceLastAssignment * 0.5; // Reduz score baseado nos dias
        }
        else
        {
            // Nunca foi designado - prioridade máxima
            score -= 1000;
        }

        // Penalidade por ter sido designado recentemente
        if (recentlyAssigned)
        {
            score += 100;
        }

        return score;
    }
}
