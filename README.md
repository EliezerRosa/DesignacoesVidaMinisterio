# Sistema de Designações da Reunião Vida e Ministério

Sistema desenvolvido em C# .NET 8.0 para automatizar designações da Reunião Vida e Ministério das Testemunhas de Jeová.

## 🎯 Funcionalidades Principais

- **Extração de Partes de PDF**: Extrai automaticamente partes e tempos de apostilas em PDF
- **Geração de Programação Semanal**: Cria documentos DOCX com a programação semanal
- **Formulários S-89**: Preenche automaticamente formulários S-89 em PDF (AcroForm)
- **Notificações WhatsApp**: Envia notificações via WhatsApp Web (Selenium)
- **Persistência Local**: Usa arquivos CSV locais para armazenar histórico e designações
- **Algoritmo de Rodízio Justo**: Distribui designações de forma equilibrada entre publicadores aprovados
- **Ajustes Manuais**: Permite ajustes manuais e registro de desistências

## 🏗️ Arquitetura

O sistema segue uma arquitetura modular com separação clara de responsabilidades:

```
src/
├── DesignacoesVidaMinisterio.Core/     # Modelos de domínio e interfaces
├── DesignacoesVidaMinisterio.Services/ # Lógica de negócio e serviços
├── DesignacoesVidaMinisterio.Data/     # Camada de persistência
└── DesignacoesVidaMinisterio.UI/       # Interface de usuário (Console)
```

## 🔧 Pré-requisitos

- .NET 8.0 SDK
- Visual Studio 2022 ou VS Code (opcional)
- Chrome Browser (para funcionalidade WhatsApp)

## 📦 Pacotes NuGet Utilizados

- **iText7**: Processamento de PDFs (extração e criação)
- **DocumentFormat.OpenXml**: Geração de documentos DOCX
- **EPPlus**: Manipulação de planilhas Excel (alternativa disponível)
- **Selenium WebDriver**: Automação do WhatsApp Web
- **xUnit**: Framework de testes

## 🚀 Como Executar

1. **Clone o repositório**:
   ```bash
   git clone <repository-url>
   cd DesignacoesVidaMinisterio
   ```

2. **Restaure as dependências**:
   ```bash
   dotnet restore
   ```

3. **Compile o projeto**:
   ```bash
   dotnet build
   ```

4. **Execute o sistema**:
   ```bash
   dotnet run --project src/DesignacoesVidaMinisterio.UI
   ```

## 📋 Funcionalidades do Menu

### 1. Gerenciar Publicadores
- Listar publicadores cadastrados
- Adicionar novos publicadores
- Configurar tipos de participação aprovados

### 2. Gerenciar Partes da Reunião
- Listar partes da reunião
- Adicionar novas partes manualmente
- Importar partes de apostilas PDF (em desenvolvimento)

### 3. Gerar Designações da Semana
- Algoritmo automático de distribuição justa
- Considera histórico de designações
- Evita designações recentes para o mesmo publicador

### 4. Visualizar Designações
- Listar designações por período
- Filtrar por data
- Ver status das designações

### 5. Gerar Documento Semanal (DOCX)
- Cria documento formatado com a programação
- Inclui participantes e ajudantes
- Pronto para impressão

### 6. Registrar Desistência
- Registra motivos de desistência
- Atualiza status da designação
- Mantém histórico para relatórios

## 📊 Estrutura de Dados

### Publisher (Publicador)
```csharp
public class Publisher
{
    public int Id { get; set; }
    public string Nome { get; set; }
    public string Email { get; set; }
    public string? Telefone { get; set; }
    public bool IsAprovado { get; set; }
    public List<TipoParticipacao> TiposAprovados { get; set; }
    public int TotalDesignacoes { get; set; }
    public DateTime? UltimaDesignacao { get; set; }
}
```

### MeetingPart (Parte da Reunião)
```csharp
public class MeetingPart
{
    public int Id { get; set; }
    public string Titulo { get; set; }
    public int DuracaoMinutos { get; set; }
    public TipoParticipacao TipoParticipacao { get; set; }
    public DateTime DataReuniao { get; set; }
    public bool RequereAjudante { get; set; }
}
```

### Assignment (Designação)
```csharp
public class Assignment
{
    public int Id { get; set; }
    public int PublisherId { get; set; }
    public int MeetingPartId { get; set; }
    public int? AjudanteId { get; set; }
    public DateTime DataDesignacao { get; set; }
    public StatusDesignacao Status { get; set; }
    public string? Observacoes { get; set; }
}
```

## 🔄 Algoritmo de Rodízio Justo

O sistema implementa um algoritmo que considera:

1. **Número total de designações**: Prioriza quem tem menos designações
2. **Tempo desde a última designação**: Favorece quem não foi designado recentemente
3. **Aprovações**: Verifica se o publicador está aprovado para o tipo de participação
4. **Conflitos de data**: Evita múltiplas designações na mesma reunião
5. **Ajudantes**: Designa automaticamente ajudantes quando necessário

## 📂 Estrutura de Arquivos

### Dados (pasta `data/`)
- `publishers.csv`: Cadastro de publicadores
- `assignments.csv`: Histórico de designações
- `meeting_parts.csv`: Partes das reuniões

### Saídas (pasta `output/`)
- `Programacao_Semanal_YYYY_MM_DD.docx`: Programação semanal
- `S89_NomePublicador_YYYY_MM_DD.pdf`: Formulários S-89

## 🔒 Considerações de Segurança

- Dados armazenados localmente em arquivos CSV
- Nenhuma informação sensível enviada para serviços externos
- WhatsApp Web requer autenticação manual por segurança

## 🚧 Funcionalidades em Desenvolvimento

- [ ] Interface gráfica (WPF/WinUI)
- [ ] Extração automática melhorada de PDFs
- [ ] Relatórios estatísticos
- [ ] Backup automático de dados
- [ ] Integração com calendários
- [ ] Notificações por email
- [ ] API REST para integração

## 🧪 Testes

Execute os testes com:
```bash
dotnet test
```

## 📄 Licença

Este projeto está licenciado sob a licença MIT. Veja o arquivo LICENSE para detalhes.

## 🤝 Contribuição

Contribuições são bem-vindas! Por favor, abra uma issue para discutir mudanças importantes antes de criar um pull request.

## 📞 Suporte

Para suporte técnico ou dúvidas, abra uma issue no repositório ou entre em contato através do email do projeto.

---

**Nota**: Este sistema foi desenvolvido para uso da comunidade das Testemunhas de Jeová e não possui fins comerciais. O uso do WhatsApp Web está sujeito aos termos de serviço do WhatsApp.
