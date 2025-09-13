using DesignacoesVidaMinisterio.Core.Interfaces;
using DesignacoesVidaMinisterio.Core.Models;
using System.Globalization;
using System.Text;

namespace DesignacoesVidaMinisterio.Data;

/// <summary>
/// Implementação do serviço de dados usando arquivos CSV
/// </summary>
public class CsvDataService : IDataService
{
    private readonly string _dataDirectory;
    private readonly string _publishersFile;
    private readonly string _assignmentsFile;
    private readonly string _meetingPartsFile;

    public CsvDataService(string dataDirectory = "data")
    {
        _dataDirectory = dataDirectory;
        _publishersFile = Path.Combine(_dataDirectory, "publishers.csv");
        _assignmentsFile = Path.Combine(_dataDirectory, "assignments.csv");
        _meetingPartsFile = Path.Combine(_dataDirectory, "meeting_parts.csv");

        EnsureDataDirectoryExists();
        InitializeFiles();
    }

    private void EnsureDataDirectoryExists()
    {
        if (!Directory.Exists(_dataDirectory))
        {
            Directory.CreateDirectory(_dataDirectory);
        }
    }

    private void InitializeFiles()
    {
        InitializePublishersFile();
        InitializeAssignmentsFile();
        InitializeMeetingPartsFile();
    }

    private void InitializePublishersFile()
    {
        if (!File.Exists(_publishersFile))
        {
            var header = "Id,Nome,Email,Telefone,IsAprovado,DataCadastro,TiposAprovados,TotalDesignacoes,UltimaDesignacao";
            File.WriteAllText(_publishersFile, header + Environment.NewLine, Encoding.UTF8);
        }
    }

    private void InitializeAssignmentsFile()
    {
        if (!File.Exists(_assignmentsFile))
        {
            var header = "Id,PublisherId,MeetingPartId,AjudanteId,DataDesignacao,Status,Observacoes";
            File.WriteAllText(_assignmentsFile, header + Environment.NewLine, Encoding.UTF8);
        }
    }

    private void InitializeMeetingPartsFile()
    {
        if (!File.Exists(_meetingPartsFile))
        {
            var header = "Id,Titulo,DuracaoMinutos,TipoParticipacao,DataReuniao,Descricao,RequereAjudante";
            File.WriteAllText(_meetingPartsFile, header + Environment.NewLine, Encoding.UTF8);
        }
    }

    public Task<List<Publisher>> GetPublishersAsync()
    {
        var publishers = new List<Publisher>();
        
        if (!File.Exists(_publishersFile))
            return Task.FromResult(publishers);

        var lines = File.ReadAllLines(_publishersFile, Encoding.UTF8);
        
        for (int i = 1; i < lines.Length; i++) // Skip header
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;

            var parts = ParseCsvLine(line);
            if (parts.Length < 8) continue;

            var publisher = new Publisher
            {
                Id = int.Parse(parts[0]),
                Nome = parts[1],
                Email = parts[2],
                Telefone = string.IsNullOrEmpty(parts[3]) ? null : parts[3],
                IsAprovado = bool.Parse(parts[4]),
                DataCadastro = DateTime.Parse(parts[5]),
                TotalDesignacoes = int.Parse(parts[7])
            };

            // Parse tipos aprovados
            if (!string.IsNullOrEmpty(parts[6]))
            {
                publisher.TiposAprovados = parts[6].Split(';')
                    .Where(t => !string.IsNullOrEmpty(t))
                    .Select(t => Enum.Parse<TipoParticipacao>(t.Trim()))
                    .ToList();
            }

            // Parse última designação
            if (parts.Length > 8 && !string.IsNullOrEmpty(parts[8]))
            {
                publisher.UltimaDesignacao = DateTime.Parse(parts[8]);
            }

            publishers.Add(publisher);
        }

        return Task.FromResult(publishers);
    }

    public async Task<Publisher> SavePublisherAsync(Publisher publisher)
    {
        var publishers = await GetPublishersAsync();
        
        if (publisher.Id == 0)
        {
            // Novo publisher - gerar ID
            publisher.Id = publishers.Any() ? publishers.Max(p => p.Id) + 1 : 1;
            publishers.Add(publisher);
        }
        else
        {
            // Atualizar publisher existente
            var index = publishers.FindIndex(p => p.Id == publisher.Id);
            if (index >= 0)
                publishers[index] = publisher;
            else
                publishers.Add(publisher);
        }

        await SavePublishersAsync(publishers);
        return publisher;
    }

    private async Task SavePublishersAsync(List<Publisher> publishers)
    {
        var lines = new List<string>
        {
            "Id,Nome,Email,Telefone,IsAprovado,DataCadastro,TiposAprovados,TotalDesignacoes,UltimaDesignacao"
        };

        foreach (var publisher in publishers)
        {
            var line = $"{publisher.Id}," +
                      $"\"{EscapeCsv(publisher.Nome)}\"," +
                      $"\"{EscapeCsv(publisher.Email)}\"," +
                      $"\"{EscapeCsv(publisher.Telefone ?? "")}\"," +
                      $"{publisher.IsAprovado}," +
                      $"{publisher.DataCadastro:yyyy-MM-dd HH:mm:ss}," +
                      $"\"{string.Join(";", publisher.TiposAprovados)}\"," +
                      $"{publisher.TotalDesignacoes}," +
                      $"{publisher.UltimaDesignacao?.ToString("yyyy-MM-dd HH:mm:ss") ?? ""}";
            lines.Add(line);
        }

        await File.WriteAllLinesAsync(_publishersFile, lines, Encoding.UTF8);
    }

    public Task<List<Assignment>> GetAssignmentsAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var assignments = new List<Assignment>();
        
        if (!File.Exists(_assignmentsFile))
            return Task.FromResult(assignments);

        var lines = File.ReadAllLines(_assignmentsFile, Encoding.UTF8);
        
        for (int i = 1; i < lines.Length; i++) // Skip header
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;

            var parts = ParseCsvLine(line);
            if (parts.Length < 6) continue;

            var dataDesignacao = DateTime.Parse(parts[4]);
            
            // Filtrar por data se especificado
            if (startDate.HasValue && dataDesignacao < startDate.Value) continue;
            if (endDate.HasValue && dataDesignacao > endDate.Value) continue;

            var assignment = new Assignment
            {
                Id = int.Parse(parts[0]),
                PublisherId = int.Parse(parts[1]),
                MeetingPartId = int.Parse(parts[2]),
                DataDesignacao = dataDesignacao,
                Status = Enum.Parse<StatusDesignacao>(parts[5]),
                Observacoes = parts.Length > 6 ? parts[6] : null
            };

            if (!string.IsNullOrEmpty(parts[3]))
            {
                assignment.AjudanteId = int.Parse(parts[3]);
            }

            assignments.Add(assignment);
        }

        return Task.FromResult(assignments);
    }

    public async Task<Assignment> SaveAssignmentAsync(Assignment assignment)
    {
        var assignments = await GetAssignmentsAsync();
        
        if (assignment.Id == 0)
        {
            assignment.Id = assignments.Any() ? assignments.Max(a => a.Id) + 1 : 1;
            assignments.Add(assignment);
        }
        else
        {
            var index = assignments.FindIndex(a => a.Id == assignment.Id);
            if (index >= 0)
                assignments[index] = assignment;
            else
                assignments.Add(assignment);
        }

        await SaveAssignmentsAsync(assignments);
        return assignment;
    }

    private async Task SaveAssignmentsAsync(List<Assignment> assignments)
    {
        var lines = new List<string>
        {
            "Id,PublisherId,MeetingPartId,AjudanteId,DataDesignacao,Status,Observacoes"
        };

        foreach (var assignment in assignments)
        {
            var line = $"{assignment.Id}," +
                      $"{assignment.PublisherId}," +
                      $"{assignment.MeetingPartId}," +
                      $"{assignment.AjudanteId ?? 0}," +
                      $"{assignment.DataDesignacao:yyyy-MM-dd HH:mm:ss}," +
                      $"{assignment.Status}," +
                      $"\"{EscapeCsv(assignment.Observacoes ?? "")}\"";
            lines.Add(line);
        }

        await File.WriteAllLinesAsync(_assignmentsFile, lines, Encoding.UTF8);
    }

    public Task<List<MeetingPart>> GetMeetingPartsAsync(DateTime? date = null)
    {
        var meetingParts = new List<MeetingPart>();
        
        if (!File.Exists(_meetingPartsFile))
            return Task.FromResult(meetingParts);

        var lines = File.ReadAllLines(_meetingPartsFile, Encoding.UTF8);
        
        for (int i = 1; i < lines.Length; i++) // Skip header
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;

            var parts = ParseCsvLine(line);
            if (parts.Length < 6) continue;

            var dataReuniao = DateTime.Parse(parts[4]);
            
            // Filtrar por data se especificado
            if (date.HasValue && dataReuniao.Date != date.Value.Date) continue;

            var meetingPart = new MeetingPart
            {
                Id = int.Parse(parts[0]),
                Titulo = parts[1],
                DuracaoMinutos = int.Parse(parts[2]),
                TipoParticipacao = Enum.Parse<TipoParticipacao>(parts[3]),
                DataReuniao = dataReuniao,
                Descricao = parts.Length > 5 ? parts[5] : null,
                RequereAjudante = parts.Length > 6 && bool.Parse(parts[6])
            };

            meetingParts.Add(meetingPart);
        }

        return Task.FromResult(meetingParts);
    }

    public async Task<MeetingPart> SaveMeetingPartAsync(MeetingPart meetingPart)
    {
        var meetingParts = await GetMeetingPartsAsync();
        
        if (meetingPart.Id == 0)
        {
            meetingPart.Id = meetingParts.Any() ? meetingParts.Max(mp => mp.Id) + 1 : 1;
            meetingParts.Add(meetingPart);
        }
        else
        {
            var index = meetingParts.FindIndex(mp => mp.Id == meetingPart.Id);
            if (index >= 0)
                meetingParts[index] = meetingPart;
            else
                meetingParts.Add(meetingPart);
        }

        await SaveMeetingPartsAsync(meetingParts);
        return meetingPart;
    }

    private async Task SaveMeetingPartsAsync(List<MeetingPart> meetingParts)
    {
        var lines = new List<string>
        {
            "Id,Titulo,DuracaoMinutos,TipoParticipacao,DataReuniao,Descricao,RequereAjudante"
        };

        foreach (var part in meetingParts)
        {
            var line = $"{part.Id}," +
                      $"\"{EscapeCsv(part.Titulo)}\"," +
                      $"{part.DuracaoMinutos}," +
                      $"{part.TipoParticipacao}," +
                      $"{part.DataReuniao:yyyy-MM-dd HH:mm:ss}," +
                      $"\"{EscapeCsv(part.Descricao ?? "")}\"," +
                      $"{part.RequereAjudante}";
            lines.Add(line);
        }

        await File.WriteAllLinesAsync(_meetingPartsFile, lines, Encoding.UTF8);
    }

    private string[] ParseCsvLine(string line)
    {
        var parts = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                parts.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        parts.Add(current.ToString());
        return parts.ToArray();
    }

    private string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        return value.Replace("\"", "\"\"");
    }
}