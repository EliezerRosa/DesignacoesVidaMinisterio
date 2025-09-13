using DesignacoesVidaMinisterio.Core.Interfaces;
using DesignacoesVidaMinisterio.Core.Models;
using DesignacoesVidaMinisterio.Data;
using DesignacoesVidaMinisterio.Services;

namespace DesignacoesVidaMinisterio.UI;

class Program
{
    private static IDataService _dataService = null!;
    private static IAssignmentService _assignmentService = null!;
    private static IDocxGenerationService _docxService = null!;

    static async Task Main(string[] args)
    {
        Console.WriteLine("=== Sistema de Designações da Reunião Vida e Ministério ===");
        Console.WriteLine();

        // Inicializar serviços
        _dataService = new CsvDataService();
        _assignmentService = new AssignmentService(_dataService);
        _docxService = new DocxGenerationService(_dataService);

        // Menu principal
        bool continuar = true;
        while (continuar)
        {
            MostrarMenuPrincipal();
            var opcao = Console.ReadLine();

            try
            {
                switch (opcao)
                {
                    case "1":
                        await GerenciarPublishers();
                        break;
                    case "2":
                        await GerenciarPartesReuniao();
                        break;
                    case "3":
                        await GerarDesignacoesSemana();
                        break;
                    case "4":
                        await VisualizarDesignacoes();
                        break;
                    case "5":
                        await GerarDocumentoSemanal();
                        break;
                    case "6":
                        await RegistrarDesistencia();
                        break;
                    case "0":
                        continuar = false;
                        break;
                    default:
                        Console.WriteLine("Opção inválida!");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro: {ex.Message}");
            }

            if (continuar)
            {
                Console.WriteLine("\nPressione qualquer tecla para continuar...");
                Console.ReadKey();
            }
        }

        Console.WriteLine("Sistema encerrado. Até logo!");
    }

    private static void MostrarMenuPrincipal()
    {
        Console.Clear();
        Console.WriteLine("=== MENU PRINCIPAL ===");
        Console.WriteLine("1. Gerenciar Publicadores");
        Console.WriteLine("2. Gerenciar Partes da Reunião");
        Console.WriteLine("3. Gerar Designações da Semana");
        Console.WriteLine("4. Visualizar Designações");
        Console.WriteLine("5. Gerar Documento Semanal (DOCX)");
        Console.WriteLine("6. Registrar Desistência");
        Console.WriteLine("0. Sair");
        Console.WriteLine();
        Console.Write("Escolha uma opção: ");
    }

    private static async Task GerenciarPublishers()
    {
        Console.Clear();
        Console.WriteLine("=== GERENCIAR PUBLICADORES ===");
        Console.WriteLine("1. Listar Publicadores");
        Console.WriteLine("2. Adicionar Publicador");
        Console.WriteLine("3. Voltar");
        Console.Write("Escolha uma opção: ");

        var opcao = Console.ReadLine();
        switch (opcao)
        {
            case "1":
                await ListarPublishers();
                break;
            case "2":
                await AdicionarPublisher();
                break;
        }
    }

    private static async Task ListarPublishers()
    {
        var publishers = await _dataService.GetPublishersAsync();
        
        Console.Clear();
        Console.WriteLine("=== LISTA DE PUBLICADORES ===");
        Console.WriteLine();

        if (!publishers.Any())
        {
            Console.WriteLine("Nenhum publicador cadastrado.");
            return;
        }

        foreach (var publisher in publishers)
        {
            Console.WriteLine($"ID: {publisher.Id}");
            Console.WriteLine($"Nome: {publisher.Nome}");
            Console.WriteLine($"Email: {publisher.Email}");
            Console.WriteLine($"Aprovado: {(publisher.IsAprovado ? "Sim" : "Não")}");
            Console.WriteLine($"Total de Designações: {publisher.TotalDesignacoes}");
            Console.WriteLine($"Tipos Aprovados: {string.Join(", ", publisher.TiposAprovados)}");
            Console.WriteLine($"Última Designação: {publisher.UltimaDesignacao?.ToString("dd/MM/yyyy") ?? "Nunca"}");
            Console.WriteLine(new string('-', 50));
        }
    }

    private static async Task AdicionarPublisher()
    {
        Console.Clear();
        Console.WriteLine("=== ADICIONAR PUBLICADOR ===");

        Console.Write("Nome: ");
        var nome = Console.ReadLine() ?? "";

        Console.Write("Email: ");
        var email = Console.ReadLine() ?? "";

        Console.Write("Telefone (opcional): ");
        var telefone = Console.ReadLine();

        Console.WriteLine("\nTipos de participação aprovados:");
        Console.WriteLine("1. Estudante");
        Console.WriteLine("2. Ajudante");
        Console.WriteLine("3. Presidente");
        Console.WriteLine("4. Leitor");
        Console.WriteLine("5. Discurso");
        Console.WriteLine("6. Demonstração");
        Console.WriteLine("7. Parte Dramática");
        Console.Write("Digite os números separados por vírgula (ex: 1,2,4): ");
        
        var tiposInput = Console.ReadLine() ?? "";
        var tiposAprovados = new List<TipoParticipacao>();
        
        foreach (var tipo in tiposInput.Split(','))
        {
            if (int.TryParse(tipo.Trim(), out int tipoNum) && tipoNum >= 1 && tipoNum <= 7)
            {
                tiposAprovados.Add((TipoParticipacao)(tipoNum - 1));
            }
        }

        var publisher = new Publisher
        {
            Nome = nome,
            Email = email,
            Telefone = string.IsNullOrWhiteSpace(telefone) ? null : telefone,
            IsAprovado = true,
            TiposAprovados = tiposAprovados,
            DataCadastro = DateTime.Now
        };

        await _dataService.SavePublisherAsync(publisher);
        Console.WriteLine("\nPublicador adicionado com sucesso!");
    }

    private static async Task GerenciarPartesReuniao()
    {
        Console.Clear();
        Console.WriteLine("=== GERENCIAR PARTES DA REUNIÃO ===");
        Console.WriteLine("1. Listar Partes");
        Console.WriteLine("2. Adicionar Parte");
        Console.WriteLine("3. Voltar");
        Console.Write("Escolha uma opção: ");

        var opcao = Console.ReadLine();
        switch (opcao)
        {
            case "1":
                await ListarPartes();
                break;
            case "2":
                await AdicionarParte();
                break;
        }
    }

    private static async Task ListarPartes()
    {
        Console.Write("Digite a data da reunião (dd/MM/yyyy) ou Enter para todas: ");
        var dataInput = Console.ReadLine();
        
        DateTime? data = null;
        if (!string.IsNullOrWhiteSpace(dataInput) && DateTime.TryParseExact(dataInput, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime parsedDate))
        {
            data = parsedDate;
        }

        var partes = await _dataService.GetMeetingPartsAsync(data);
        
        Console.Clear();
        Console.WriteLine("=== PARTES DA REUNIÃO ===");
        Console.WriteLine();

        if (!partes.Any())
        {
            Console.WriteLine("Nenhuma parte encontrada.");
            return;
        }

        foreach (var parte in partes.OrderBy(p => p.DataReuniao))
        {
            Console.WriteLine($"ID: {parte.Id}");
            Console.WriteLine($"Título: {parte.Titulo}");
            Console.WriteLine($"Duração: {parte.DuracaoMinutos} minutos");
            Console.WriteLine($"Tipo: {parte.TipoParticipacao}");
            Console.WriteLine($"Data: {parte.DataReuniao:dd/MM/yyyy}");
            Console.WriteLine($"Requer Ajudante: {(parte.RequereAjudante ? "Sim" : "Não")}");
            if (!string.IsNullOrEmpty(parte.Descricao))
                Console.WriteLine($"Descrição: {parte.Descricao}");
            Console.WriteLine(new string('-', 50));
        }
    }

    private static async Task AdicionarParte()
    {
        Console.Clear();
        Console.WriteLine("=== ADICIONAR PARTE DA REUNIÃO ===");

        Console.Write("Título: ");
        var titulo = Console.ReadLine() ?? "";

        Console.Write("Duração (minutos): ");
        var duracao = int.TryParse(Console.ReadLine(), out int min) ? min : 5;

        Console.Write("Data da reunião (dd/MM/yyyy): ");
        var dataInput = Console.ReadLine() ?? "";
        if (!DateTime.TryParseExact(dataInput, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime dataReuniao))
        {
            Console.WriteLine("Data inválida!");
            return;
        }

        Console.WriteLine("\nTipo de participação:");
        var tipos = Enum.GetValues<TipoParticipacao>();
        for (int i = 0; i < tipos.Length; i++)
        {
            Console.WriteLine($"{i + 1}. {tipos[i]}");
        }
        Console.Write("Escolha o tipo: ");
        var tipoEscolhido = int.TryParse(Console.ReadLine(), out int tipoNum) && tipoNum >= 1 && tipoNum <= tipos.Length
            ? tipos[tipoNum - 1]
            : TipoParticipacao.Estudante;

        Console.Write("Descrição (opcional): ");
        var descricao = Console.ReadLine();

        Console.Write("Requer ajudante? (s/n): ");
        var requereAjudante = Console.ReadLine()?.ToLower() == "s";

        var parte = new MeetingPart
        {
            Titulo = titulo,
            DuracaoMinutos = duracao,
            DataReuniao = dataReuniao,
            TipoParticipacao = tipoEscolhido,
            Descricao = string.IsNullOrWhiteSpace(descricao) ? null : descricao,
            RequereAjudante = requereAjudante
        };

        await _dataService.SaveMeetingPartAsync(parte);
        Console.WriteLine("\nParte adicionada com sucesso!");
    }

    private static async Task GerarDesignacoesSemana()
    {
        Console.Clear();
        Console.WriteLine("=== GERAR DESIGNAÇÕES DA SEMANA ===");

        Console.Write("Digite a data da semana (dd/MM/yyyy): ");
        var dataInput = Console.ReadLine() ?? "";
        
        if (!DateTime.TryParseExact(dataInput, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime weekDate))
        {
            Console.WriteLine("Data inválida!");
            return;
        }

        Console.WriteLine("\nGerando designações...");
        var assignments = await _assignmentService.GenerateWeeklyAssignmentsAsync(weekDate);

        Console.WriteLine($"\n{assignments.Count} designações geradas com sucesso!");
        
        foreach (var assignment in assignments)
        {
            var meetingPart = (await _dataService.GetMeetingPartsAsync()).First(mp => mp.Id == assignment.MeetingPartId);
            var publisher = (await _dataService.GetPublishersAsync()).First(p => p.Id == assignment.PublisherId);
            
            Console.WriteLine($"- {meetingPart.Titulo}: {publisher.Nome}");
        }
    }

    private static async Task VisualizarDesignacoes()
    {
        Console.Clear();
        Console.WriteLine("=== VISUALIZAR DESIGNAÇÕES ===");

        Console.Write("Data inicial (dd/MM/yyyy) ou Enter para esta semana: ");
        var dataInicialInput = Console.ReadLine();
        
        DateTime dataInicial = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
        if (!string.IsNullOrWhiteSpace(dataInicialInput) && DateTime.TryParseExact(dataInicialInput, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime parsedInicial))
        {
            dataInicial = parsedInicial;
        }

        var dataFinal = dataInicial.AddDays(7);
        var assignments = await _dataService.GetAssignmentsAsync(dataInicial, dataFinal);
        var meetingParts = await _dataService.GetMeetingPartsAsync();
        var publishers = await _dataService.GetPublishersAsync();

        Console.WriteLine($"\n=== DESIGNAÇÕES DE {dataInicial:dd/MM/yyyy} A {dataFinal:dd/MM/yyyy} ===");
        Console.WriteLine();

        if (!assignments.Any())
        {
            Console.WriteLine("Nenhuma designação encontrada para este período.");
            return;
        }

        foreach (var assignment in assignments.OrderBy(a => a.DataDesignacao))
        {
            var part = meetingParts.FirstOrDefault(mp => mp.Id == assignment.MeetingPartId);
            var publisher = publishers.FirstOrDefault(p => p.Id == assignment.PublisherId);
            var ajudante = assignment.AjudanteId.HasValue ? publishers.FirstOrDefault(p => p.Id == assignment.AjudanteId.Value) : null;

            Console.WriteLine($"Data: {part?.DataReuniao:dd/MM/yyyy}");
            Console.WriteLine($"Parte: {part?.Titulo}");
            Console.WriteLine($"Participante: {publisher?.Nome}");
            if (ajudante != null)
                Console.WriteLine($"Ajudante: {ajudante.Nome}");
            Console.WriteLine($"Status: {assignment.Status}");
            if (!string.IsNullOrEmpty(assignment.Observacoes))
                Console.WriteLine($"Observações: {assignment.Observacoes}");
            Console.WriteLine(new string('-', 50));
        }
    }

    private static async Task GerarDocumentoSemanal()
    {
        Console.Clear();
        Console.WriteLine("=== GERAR DOCUMENTO SEMANAL ===");

        Console.Write("Digite a data da semana (dd/MM/yyyy): ");
        var dataInput = Console.ReadLine() ?? "";
        
        if (!DateTime.TryParseExact(dataInput, "dd/MM/yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime weekDate))
        {
            Console.WriteLine("Data inválida!");
            return;
        }

        var assignments = await _dataService.GetAssignmentsAsync(weekDate, weekDate.AddDays(7));
        
        if (!assignments.Any())
        {
            Console.WriteLine("Nenhuma designação encontrada para esta semana. Gere as designações primeiro.");
            return;
        }

        Console.WriteLine("\nGerando documento DOCX...");
        var filePath = await _docxService.GenerateWeeklyProgramAsync(assignments, weekDate);
        
        Console.WriteLine($"\nDocumento gerado com sucesso: {filePath}");
    }

    private static async Task RegistrarDesistencia()
    {
        Console.Clear();
        Console.WriteLine("=== REGISTRAR DESISTÊNCIA ===");

        Console.Write("ID da designação: ");
        if (!int.TryParse(Console.ReadLine(), out int assignmentId))
        {
            Console.WriteLine("ID inválido!");
            return;
        }

        Console.Write("Motivo da desistência: ");
        var motivo = Console.ReadLine() ?? "";

        await _assignmentService.RegisterDropoutAsync(assignmentId, motivo);
        Console.WriteLine("\nDesistência registrada com sucesso!");
    }
}
