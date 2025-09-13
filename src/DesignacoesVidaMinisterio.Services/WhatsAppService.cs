using DesignacoesVidaMinisterio.Core.Interfaces;
using DesignacoesVidaMinisterio.Core.Models;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;

namespace DesignacoesVidaMinisterio.Services;

/// <summary>
/// Serviço para envio de notificações via WhatsApp Web (usando Selenium)
/// </summary>
public class WhatsAppService : IWhatsAppService
{
    private readonly bool _isEnabled;

    public WhatsAppService(bool isEnabled = false)
    {
        _isEnabled = isEnabled;
    }

    public async Task SendNotificationAsync(string phoneNumber, string message)
    {
        if (!_isEnabled)
        {
            Console.WriteLine($"[SIMULAÇÃO] WhatsApp para {phoneNumber}: {message}");
            await Task.Delay(100); // Simular delay de envio
            return;
        }

        // Implementação real com Selenium (desabilitada por padrão)
        try
        {
            await SendWhatsAppMessageAsync(phoneNumber, message);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Erro ao enviar mensagem WhatsApp: {ex.Message}", ex);
        }
    }

    public async Task SendAssignmentNotificationAsync(Assignment assignment)
    {
        if (assignment.Publisher == null || assignment.MeetingPart == null)
        {
            throw new ArgumentException("Assignment deve ter Publisher e MeetingPart carregados");
        }

        var phoneNumber = assignment.Publisher.Telefone;
        if (string.IsNullOrEmpty(phoneNumber))
        {
            Console.WriteLine($"Publisher {assignment.Publisher.Nome} não tem telefone cadastrado");
            return;
        }

        var message = CreateAssignmentMessage(assignment);
        await SendNotificationAsync(phoneNumber, message);

        // Se há ajudante, enviar notificação também
        if (assignment.AjudanteId.HasValue && assignment.Ajudante != null)
        {
            var ajudantePhone = assignment.Ajudante.Telefone;
            if (!string.IsNullOrEmpty(ajudantePhone))
            {
                var ajudanteMessage = CreateHelperMessage(assignment);
                await SendNotificationAsync(ajudantePhone, ajudanteMessage);
            }
        }
    }

    private string CreateAssignmentMessage(Assignment assignment)
    {
        var message = $"🙋‍♂️ *Nova Designação - Reunião Vida e Ministério*\n\n";
        message += $"📅 *Data:* {assignment.MeetingPart?.DataReuniao:dd/MM/yyyy}\n";
        message += $"📝 *Parte:* {assignment.MeetingPart?.Titulo}\n";
        message += $"⏱️ *Duração:* {assignment.MeetingPart?.DuracaoMinutos} minutos\n";
        message += $"👤 *Tipo:* {assignment.MeetingPart?.TipoParticipacao}\n";

        if (assignment.AjudanteId.HasValue && assignment.Ajudante != null)
        {
            message += $"🤝 *Ajudante:* {assignment.Ajudante.Nome}\n";
        }

        if (!string.IsNullOrEmpty(assignment.MeetingPart?.Descricao))
        {
            message += $"📋 *Descrição:* {assignment.MeetingPart.Descricao}\n";
        }

        message += "\n⚠️ *Lembrete:* Confirme sua participação até terça-feira da semana anterior.";
        message += "\nEm caso de impossibilidade, comunique imediatamente.";

        return message;
    }

    private string CreateHelperMessage(Assignment assignment)
    {
        var message = $"🤝 *Designação como Ajudante - Reunião Vida e Ministério*\n\n";
        message += $"📅 *Data:* {assignment.MeetingPart?.DataReuniao:dd/MM/yyyy}\n";
        message += $"📝 *Parte:* {assignment.MeetingPart?.Titulo}\n";
        message += $"👤 *Estudante Principal:* {assignment.Publisher?.Nome}\n";
        message += $"⏱️ *Duração:* {assignment.MeetingPart?.DuracaoMinutos} minutos\n";

        message += "\n⚠️ *Lembrete:* Confirme sua participação até terça-feira da semana anterior.";
        message += "\nCoordenhe com o estudante principal para preparação.";

        return message;
    }

    private async Task SendWhatsAppMessageAsync(string phoneNumber, string message)
    {
        // Esta implementação requer o Chrome e o ChromeDriver
        // Em ambiente de produção, seria necessário configurar adequadamente
        
        var chromeOptions = new ChromeOptions();
        chromeOptions.AddArguments("--headless"); // Executar sem interface gráfica
        chromeOptions.AddArguments("--no-sandbox");
        chromeOptions.AddArguments("--disable-dev-shm-usage");

        IWebDriver? driver = null;
        try
        {
            driver = new ChromeDriver(chromeOptions);
            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));

            // Navegar para WhatsApp Web
            driver.Navigate().GoToUrl("https://web.whatsapp.com");

            // Aguardar login manual (em produção, implementar autenticação automática)
            Console.WriteLine("Por favor, escaneie o QR code no WhatsApp Web...");
            await Task.Delay(30000); // Tempo para login manual

            // Procurar contato
            var searchBox = wait.Until(d => d.FindElement(By.XPath("//div[@contenteditable='true'][@data-tab='3']")));
            searchBox.SendKeys(phoneNumber);
            await Task.Delay(2000);
            searchBox.SendKeys(Keys.Enter);

            // Enviar mensagem
            var messageBox = wait.Until(d => d.FindElement(By.XPath("//div[@contenteditable='true'][@data-tab='6']")));
            messageBox.SendKeys(message);
            await Task.Delay(1000);
            messageBox.SendKeys(Keys.Enter);

            await Task.Delay(2000); // Aguardar envio
        }
        finally
        {
            driver?.Quit();
        }
    }
}