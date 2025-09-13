using DesignacoesVidaMinisterio.Core.Interfaces;
using DesignacoesVidaMinisterio.Core.Models;
using OfficeOpenXml;
using System.Globalization;

namespace DesignacoesVidaMinisterio.Data;

/// <summary>
/// Implementação do serviço de dados usando planilhas Excel
/// </summary>
public class ExcelDataService : IDataService
{
    private readonly string _dataDirectory;
    private readonly string _publishersFile;
    private readonly string _assignmentsFile;
    private readonly string _meetingPartsFile;

    public ExcelDataService(string dataDirectory = "data")
    {
        _dataDirectory = dataDirectory;
        _publishersFile = Path.Combine(_dataDirectory, "publishers.xlsx");
        _assignmentsFile = Path.Combine(_dataDirectory, "assignments.xlsx");
        _meetingPartsFile = Path.Combine(_dataDirectory, "meeting_parts.xlsx");

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
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Publishers");
            worksheet.Cells[1, 1].Value = "Id";
            worksheet.Cells[1, 2].Value = "Nome";
            worksheet.Cells[1, 3].Value = "Email";
            worksheet.Cells[1, 4].Value = "Telefone";
            worksheet.Cells[1, 5].Value = "IsAprovado";
            worksheet.Cells[1, 6].Value = "DataCadastro";
            worksheet.Cells[1, 7].Value = "TiposAprovados";
            worksheet.Cells[1, 8].Value = "TotalDesignacoes";
            worksheet.Cells[1, 9].Value = "UltimaDesignacao";

            package.SaveAs(_publishersFile);
        }
    }

    private void InitializeAssignmentsFile()
    {
        if (!File.Exists(_assignmentsFile))
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Assignments");
            worksheet.Cells[1, 1].Value = "Id";
            worksheet.Cells[1, 2].Value = "PublisherId";
            worksheet.Cells[1, 3].Value = "MeetingPartId";
            worksheet.Cells[1, 4].Value = "AjudanteId";
            worksheet.Cells[1, 5].Value = "DataDesignacao";
            worksheet.Cells[1, 6].Value = "Status";
            worksheet.Cells[1, 7].Value = "Observacoes";

            package.SaveAs(_assignmentsFile);
        }
    }

    private void InitializeMeetingPartsFile()
    {
        if (!File.Exists(_meetingPartsFile))
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("MeetingParts");
            worksheet.Cells[1, 1].Value = "Id";
            worksheet.Cells[1, 2].Value = "Titulo";
            worksheet.Cells[1, 3].Value = "DuracaoMinutos";
            worksheet.Cells[1, 4].Value = "TipoParticipacao";
            worksheet.Cells[1, 5].Value = "DataReuniao";
            worksheet.Cells[1, 6].Value = "Descricao";
            worksheet.Cells[1, 7].Value = "RequereAjudante";

            package.SaveAs(_meetingPartsFile);
        }
    }

    public Task<List<Publisher>> GetPublishersAsync()
    {
        var publishers = new List<Publisher>();
        
        using var package = new ExcelPackage(new FileInfo(_publishersFile));
        var worksheet = package.Workbook.Worksheets["Publishers"];
        
        for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
        {
            var publisher = new Publisher
            {
                Id = int.Parse(worksheet.Cells[row, 1].Text),
                Nome = worksheet.Cells[row, 2].Text,
                Email = worksheet.Cells[row, 3].Text,
                Telefone = worksheet.Cells[row, 4].Text,
                IsAprovado = bool.Parse(worksheet.Cells[row, 5].Text),
                DataCadastro = DateTime.Parse(worksheet.Cells[row, 6].Text),
                TotalDesignacoes = int.Parse(worksheet.Cells[row, 8].Text)
            };

            // Parse tipos aprovados
            var tiposText = worksheet.Cells[row, 7].Text;
            if (!string.IsNullOrEmpty(tiposText))
            {
                publisher.TiposAprovados = tiposText.Split(',')
                    .Select(t => Enum.Parse<TipoParticipacao>(t.Trim()))
                    .ToList();
            }

            // Parse última designação
            var ultimaDesignacaoText = worksheet.Cells[row, 9].Text;
            if (!string.IsNullOrEmpty(ultimaDesignacaoText))
            {
                publisher.UltimaDesignacao = DateTime.Parse(ultimaDesignacaoText);
            }

            publishers.Add(publisher);
        }

        return Task.FromResult(publishers);
    }

    public async Task<Publisher> SavePublisherAsync(Publisher publisher)
    {
        using var package = new ExcelPackage(new FileInfo(_publishersFile));
        var worksheet = package.Workbook.Worksheets["Publishers"];

        int newRow;
        if (publisher.Id == 0)
        {
            // Novo publisher - gerar ID
            publisher.Id = GetNextId(worksheet);
            newRow = worksheet.Dimension.End.Row + 1;
        }
        else
        {
            // Atualizar publisher existente
            newRow = FindRowById(worksheet, publisher.Id);
            if (newRow == -1)
            {
                newRow = worksheet.Dimension.End.Row + 1;
            }
        }

        worksheet.Cells[newRow, 1].Value = publisher.Id;
        worksheet.Cells[newRow, 2].Value = publisher.Nome;
        worksheet.Cells[newRow, 3].Value = publisher.Email;
        worksheet.Cells[newRow, 4].Value = publisher.Telefone;
        worksheet.Cells[newRow, 5].Value = publisher.IsAprovado;
        worksheet.Cells[newRow, 6].Value = publisher.DataCadastro;
        worksheet.Cells[newRow, 7].Value = string.Join(",", publisher.TiposAprovados);
        worksheet.Cells[newRow, 8].Value = publisher.TotalDesignacoes;
        worksheet.Cells[newRow, 9].Value = publisher.UltimaDesignacao;

        await package.SaveAsync();
        return publisher;
    }

    public Task<List<Assignment>> GetAssignmentsAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var assignments = new List<Assignment>();
        
        using var package = new ExcelPackage(new FileInfo(_assignmentsFile));
        var worksheet = package.Workbook.Worksheets["Assignments"];
        
        if (worksheet.Dimension == null) return Task.FromResult(assignments);

        for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
        {
            var dataDesignacao = DateTime.Parse(worksheet.Cells[row, 5].Text);
            
            // Filtrar por data se especificado
            if (startDate.HasValue && dataDesignacao < startDate.Value) continue;
            if (endDate.HasValue && dataDesignacao > endDate.Value) continue;

            var assignment = new Assignment
            {
                Id = int.Parse(worksheet.Cells[row, 1].Text),
                PublisherId = int.Parse(worksheet.Cells[row, 2].Text),
                MeetingPartId = int.Parse(worksheet.Cells[row, 3].Text),
                DataDesignacao = dataDesignacao,
                Status = Enum.Parse<StatusDesignacao>(worksheet.Cells[row, 6].Text),
                Observacoes = worksheet.Cells[row, 7].Text
            };

            var ajudanteText = worksheet.Cells[row, 4].Text;
            if (!string.IsNullOrEmpty(ajudanteText))
            {
                assignment.AjudanteId = int.Parse(ajudanteText);
            }

            assignments.Add(assignment);
        }

        return Task.FromResult(assignments);
    }

    public async Task<Assignment> SaveAssignmentAsync(Assignment assignment)
    {
        using var package = new ExcelPackage(new FileInfo(_assignmentsFile));
        var worksheet = package.Workbook.Worksheets["Assignments"];

        int newRow;
        if (assignment.Id == 0)
        {
            assignment.Id = GetNextId(worksheet);
            newRow = worksheet.Dimension?.End.Row + 1 ?? 2;
        }
        else
        {
            newRow = FindRowById(worksheet, assignment.Id);
            if (newRow == -1)
            {
                newRow = worksheet.Dimension?.End.Row + 1 ?? 2;
            }
        }

        worksheet.Cells[newRow, 1].Value = assignment.Id;
        worksheet.Cells[newRow, 2].Value = assignment.PublisherId;
        worksheet.Cells[newRow, 3].Value = assignment.MeetingPartId;
        worksheet.Cells[newRow, 4].Value = assignment.AjudanteId;
        worksheet.Cells[newRow, 5].Value = assignment.DataDesignacao;
        worksheet.Cells[newRow, 6].Value = assignment.Status.ToString();
        worksheet.Cells[newRow, 7].Value = assignment.Observacoes;

        await package.SaveAsync();
        return assignment;
    }

    public Task<List<MeetingPart>> GetMeetingPartsAsync(DateTime? date = null)
    {
        var meetingParts = new List<MeetingPart>();
        
        using var package = new ExcelPackage(new FileInfo(_meetingPartsFile));
        var worksheet = package.Workbook.Worksheets["MeetingParts"];
        
        if (worksheet.Dimension == null) return Task.FromResult(meetingParts);

        for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
        {
            var dataReuniao = DateTime.Parse(worksheet.Cells[row, 5].Text);
            
            // Filtrar por data se especificado
            if (date.HasValue && dataReuniao.Date != date.Value.Date) continue;

            var meetingPart = new MeetingPart
            {
                Id = int.Parse(worksheet.Cells[row, 1].Text),
                Titulo = worksheet.Cells[row, 2].Text,
                DuracaoMinutos = int.Parse(worksheet.Cells[row, 3].Text),
                TipoParticipacao = Enum.Parse<TipoParticipacao>(worksheet.Cells[row, 4].Text),
                DataReuniao = dataReuniao,
                Descricao = worksheet.Cells[row, 6].Text,
                RequereAjudante = bool.Parse(worksheet.Cells[row, 7].Text)
            };

            meetingParts.Add(meetingPart);
        }

        return Task.FromResult(meetingParts);
    }

    public async Task<MeetingPart> SaveMeetingPartAsync(MeetingPart meetingPart)
    {
        using var package = new ExcelPackage(new FileInfo(_meetingPartsFile));
        var worksheet = package.Workbook.Worksheets["MeetingParts"];

        int newRow;
        if (meetingPart.Id == 0)
        {
            meetingPart.Id = GetNextId(worksheet);
            newRow = worksheet.Dimension?.End.Row + 1 ?? 2;
        }
        else
        {
            newRow = FindRowById(worksheet, meetingPart.Id);
            if (newRow == -1)
            {
                newRow = worksheet.Dimension?.End.Row + 1 ?? 2;
            }
        }

        worksheet.Cells[newRow, 1].Value = meetingPart.Id;
        worksheet.Cells[newRow, 2].Value = meetingPart.Titulo;
        worksheet.Cells[newRow, 3].Value = meetingPart.DuracaoMinutos;
        worksheet.Cells[newRow, 4].Value = meetingPart.TipoParticipacao.ToString();
        worksheet.Cells[newRow, 5].Value = meetingPart.DataReuniao;
        worksheet.Cells[newRow, 6].Value = meetingPart.Descricao;
        worksheet.Cells[newRow, 7].Value = meetingPart.RequereAjudante;

        await package.SaveAsync();
        return meetingPart;
    }

    private int GetNextId(ExcelWorksheet worksheet)
    {
        if (worksheet.Dimension == null) return 1;
        
        int maxId = 0;
        for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
        {
            if (int.TryParse(worksheet.Cells[row, 1].Text, out int id) && id > maxId)
            {
                maxId = id;
            }
        }
        return maxId + 1;
    }

    private int FindRowById(ExcelWorksheet worksheet, int id)
    {
        if (worksheet.Dimension == null) return -1;
        
        for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
        {
            if (int.TryParse(worksheet.Cells[row, 1].Text, out int rowId) && rowId == id)
            {
                return row;
            }
        }
        return -1;
    }
}
