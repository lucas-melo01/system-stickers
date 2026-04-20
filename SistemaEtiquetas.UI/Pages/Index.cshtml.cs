using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SistemaEtiquetas.UI.Services;
using SistemaEtiquetas.Domain.Entities;
using SistemaEtiquetas.Infrastructure.Data;
using System.ComponentModel.DataAnnotations;

namespace SistemaEtiquetas.UI.Pages;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly EtiquetaService _etiquetaService;
    private readonly ImpressaoService _impressaoService;

    public List<Pedido> Pedidos { get; set; }
    public List<PedidoItemExibicao> PedidoItens { get; set; }

    [BindProperty(SupportsGet = true)]
    public string SearchTerm { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? DataPedido { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    [BindProperty]
    public AddPedidoModel AddPedidoModel { get; set; }

    [BindProperty]
    public int PedidoId { get; set; }
    [BindProperty]
    public int ItemId { get; set; }

    public string ErrorMessage { get; set; }
    public string SuccessMessage { get; set; }
    public string ZplCode { get; set; }

    // Propriedades de paginação
    public int TotalPages { get; set; }
    public int TotalItems { get; set; }
    public const int ItemsPerPage = 15;

    public IndexModel(AppDbContext db, EtiquetaService etiquetaService, ImpressaoService impressaoService)
    {
        _db = db;
        _etiquetaService = etiquetaService;
        _impressaoService = impressaoService;
    }

    public async Task OnGet()
    {
        // Validar número da página
        if (PageNumber < 1)
            PageNumber = 1;

        var query = _db.Pedidos
            .Include(p => p.Itens)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            var searchLower = SearchTerm.ToLower().Trim();
            query = query.Where(p =>
                p.PedidoExternoId.ToLower().Contains(searchLower) ||
                p.NomeCliente.ToLower().Contains(searchLower) ||
                p.ClienteCpf.ToLower().Contains(searchLower)
            );
        }

        if (DataPedido.HasValue)
        {
            var dataFiltro = DateTime.SpecifyKind(DataPedido.Value.Date, DateTimeKind.Utc);
            var proximoDia = dataFiltro.AddDays(1);
            query = query.Where(p => p.DataPedido >= dataFiltro && p.DataPedido < proximoDia);
        }

        var pedidos = await query
            .OrderByDescending(p => p.DataPedido)
            .ToListAsync();

        // Expandir para um registro por item
        var todosOsItens = new List<PedidoItemExibicao>();
        foreach (var pedido in pedidos)
        {
            foreach (var item in pedido.Itens)
            {
                todosOsItens.Add(new PedidoItemExibicao
                {
                    PedidoId = pedido.Id,
                    PedidoItemId = item.Id,
                    DataPedido = pedido.DataPedido,
                    PedidoExternoId = pedido.PedidoExternoId,
                    NomeCliente = pedido.NomeCliente,
                    ClienteCpf = pedido.ClienteCpf,
                    Produto = item.Produto,
                    Cor = item.Cor,
                    Tamanho = item.Tamanho,
                    Quantidade = item.Quantidade,
                    Impresso = item.Impresso
                });
            }
        }

        // Calcular paginação
        TotalItems = todosOsItens.Count;
        TotalPages = (int)Math.Ceiling((double)TotalItems / ItemsPerPage);

        // Validar página
        if (PageNumber > TotalPages && TotalPages > 0)
            PageNumber = TotalPages;

        // Aplicar paginação
        PedidoItens = todosOsItens
            .Skip((PageNumber - 1) * ItemsPerPage)
            .Take(ItemsPerPage)
            .ToList();
    }

    public async Task<IActionResult> OnPostAddPedido()
    {
        try
        {
            if (AddPedidoModel?.Itens == null || AddPedidoModel.Itens.Count == 0)
            {
                ErrorMessage = "É necessário adicionar pelo menos um item ao pedido.";
                return await ReturnOnGetResult();
            }

            // Validar se já existe pedido com este IdExterno
            var pedidoExistente = await _db.Pedidos
                .FirstOrDefaultAsync(p => p.PedidoExternoId == AddPedidoModel.IdExterno);

            if (pedidoExistente != null)
            {
                ErrorMessage = $"Já existe um pedido com o ID externo {AddPedidoModel.IdExterno}.";
                return await ReturnOnGetResult();
            }

            // Criar novo pedido
            var pedido = new Pedido
            {
                PedidoExternoId = AddPedidoModel.IdExterno,
                NomeCliente = AddPedidoModel.NomeCliente,
                ClienteCpf = AddPedidoModel.ClienteCpf,
                Vendedor = AddPedidoModel.Vendedor ?? "Manual",
                TipoEnvio = AddPedidoModel.TipoEnvio,
                FormaPagamento = AddPedidoModel.FormaPagamento,
                ValorFrete = AddPedidoModel.ValorFrete,
                DataPedido = AddPedidoModel.DataPedido.ToUniversalTime(),
                DataCriacao = DateTime.UtcNow,
                Itens = new List<PedidoItem>()
            };

            // Adicionar itens
            foreach (var item in AddPedidoModel.Itens.Where(i => !string.IsNullOrWhiteSpace(i.Produto)))
            {
                pedido.Itens.Add(new PedidoItem
                {
                    Produto = item.Produto,
                    SKU = item.SKU,
                    Cor = item.Cor,
                    Tamanho = item.Tamanho,
                    Quantidade = item.Quantidade > 0 ? item.Quantidade : 1,
                    ValorCusto = item.ValorCusto,
                    ValorVenda = item.ValorVenda,
                    Impresso = false
                });
            }

            _db.Pedidos.Add(pedido);
            await _db.SaveChangesAsync();

            SuccessMessage = $"Pedido {pedido.Id} criado com sucesso com {pedido.Itens.Count} item(ns)!";
            return await ReturnOnGetResult();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao salvar pedido: {ex.Message}";
            return await ReturnOnGetResult();
        }
    }

    private async Task<IActionResult> ReturnOnGetResult()
    {
        var query = _db.Pedidos
            .Include(p => p.Itens)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            var searchLower = SearchTerm.ToLower().Trim();
            query = query.Where(p =>
                p.PedidoExternoId.ToLower().Contains(searchLower) ||
                p.NomeCliente.ToLower().Contains(searchLower) ||
                p.ClienteCpf.ToLower().Contains(searchLower)
            );
        }

        if (DataPedido.HasValue)
        {
            var dataFiltro = DateTime.SpecifyKind(DataPedido.Value.Date, DateTimeKind.Utc);
            var proximoDia = dataFiltro.AddDays(1);
            query = query.Where(p => p.DataPedido >= dataFiltro && p.DataPedido < proximoDia);
        }

        var pedidos = await query
            .OrderByDescending(p => p.DataPedido)
            .ToListAsync();

        // Expandir para um registro por item
        PedidoItens = new List<PedidoItemExibicao>();
        foreach (var pedido in pedidos)
        {
            foreach (var item in pedido.Itens)
            {
                PedidoItens.Add(new PedidoItemExibicao
                {
                    PedidoId = pedido.Id,
                    PedidoItemId = item.Id,
                    DataPedido = pedido.DataPedido,
                    PedidoExternoId = pedido.PedidoExternoId,
                    NomeCliente = pedido.NomeCliente,
                    ClienteCpf = pedido.ClienteCpf,
                    Produto = item.Produto,
                    Cor = item.Cor,
                    Tamanho = item.Tamanho,
                    Quantidade = item.Quantidade,
                    Impresso = item.Impresso
                });
            }
        }

        return Page();
    }

    // Método para aplicar migrações em runtime
    public async Task OnGetMigrate()
    {
        using var scope = HttpContext.RequestServices.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }

    public async Task<IActionResult> OnPostEditPedidoItem(int pedidoId, int itemId, string produto, string cor, string tamanho, int quantidade)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(produto))
            {
                ErrorMessage = "Produto é obrigatório.";
                return await ReturnOnGetResult();
            }

            var pedido = await _db.Pedidos
                .Include(p => p.Itens)
                .FirstOrDefaultAsync(p => p.Id == pedidoId);

            if (pedido == null)
            {
                ErrorMessage = "Pedido não encontrado.";
                return await ReturnOnGetResult();
            }

            var item = pedido.Itens.FirstOrDefault(i => i.Id == itemId);
            if (item == null)
            {
                ErrorMessage = "Item não encontrado.";
                return await ReturnOnGetResult();
            }

            // Atualizar os dados do item
            item.Produto = produto;
            item.Cor = string.IsNullOrWhiteSpace(cor) ? null : cor;
            item.Tamanho = string.IsNullOrWhiteSpace(tamanho) ? null : tamanho;
            item.Quantidade = quantidade > 0 ? quantidade : 1;

            await _db.SaveChangesAsync();

            SuccessMessage = $"Item do pedido {pedido.PedidoExternoId} atualizado com sucesso!";
            return await ReturnOnGetResult();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao atualizar item: {ex.Message}";
            return await ReturnOnGetResult();
        }
    }

    public async Task<IActionResult> OnGetPrint(int pedidoId, int itemId)
    {
        try
        {
            var pedido = await _db.Pedidos
                .Include(p => p.Itens)
                .FirstOrDefaultAsync(p => p.Id == pedidoId);

            if (pedido == null)
            {
                return new JsonResult(new 
                { 
                    success = false,
                    error = "Pedido não encontrado."
                });
            }

            var item = pedido.Itens.FirstOrDefault(i => i.Id == itemId);
            if (item == null)
            {
                return new JsonResult(new 
                { 
                    success = false,
                    error = "Item não encontrado."
                });
            }

            // Gerar ZPL
            var zpl = _etiquetaService.GerarZpl(pedido, item);

            // Retornar JSON com o ZPL para exibição/teste
            return new JsonResult(new 
            { 
                success = true,
                zpl = zpl,
                pedidoExternoId = pedido.PedidoExternoId,
                produto = item.Produto,
                cliente = pedido.NomeCliente
            });
        }
        catch (Exception ex)
        {
            return new JsonResult(new 
            { 
                success = false,
                error = $"Erro ao gerar etiqueta: {ex.Message}"
            });
        }
    }

    public async Task<IActionResult> OnPostConfirmPrint()
    {
        try
        {
            var pedido = await _db.Pedidos
                .Include(p => p.Itens)
                .FirstOrDefaultAsync(p => p.Id == PedidoId);

            if (pedido == null)
            {
                return new JsonResult(new { success = false, error = "Pedido não encontrado." });
            }

            var item = pedido.Itens.FirstOrDefault(i => i.Id == ItemId);
            if (item == null)
            {
                return new JsonResult(new { success = false, error = "Item não encontrado." });
            }

            // Gerar ZPL
            var zpl = _etiquetaService.GerarZpl(pedido, item);

            // Enviar para impressora
            if (_impressaoService == null)
            {
                return new JsonResult(new { success = false, error = "Serviço de impressão não configurado." });
            }
            if (!_impressaoService.Imprimir(zpl, out var erro))
            {
                return new JsonResult(new { success = false, error = $"Erro ao imprimir: {erro}" });
            }

            // Marcar como impresso
            item.Impresso = true;
            await _db.SaveChangesAsync();

            return new JsonResult(new { success = true, message = "Etiqueta impressa e marcada como impressa!" });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, error = $"Erro: {ex.Message}" });
        }
    }

    public async Task<IActionResult> OnPostImprimirTodas()
    {
        try
        {
            var itensPendentes = await _db.PedidoItens
                .Include(i => i.Pedido)
                .Where(i => !i.Impresso)
                .ToListAsync();

            if (!itensPendentes.Any())
            {
                return new JsonResult(new { success = false, error = "Nenhuma etiqueta pendente para imprimir." });
            }

            int impressos = 0;
            var erros = new List<string>();

            foreach (var item in itensPendentes)
            {
                try
                {
                    // Gerar ZPL
                    var zpl = _etiquetaService.GerarZpl(item.Pedido, item);

                    // Enviar para impressora
                    string erro = null;
                    if (_impressaoService != null && _impressaoService.Imprimir(zpl, out erro))
                    {
                        item.Impresso = true;
                        impressos++;
                    }
                    else
                    {
                        var mensagemErro = string.IsNullOrEmpty(erro) ? "Erro desconhecido" : erro;
                        erros.Add($"Item {item.Id}: {mensagemErro}");
                    }
                }
                catch (Exception ex)
                {
                    erros.Add($"Item {item.Id}: {ex.Message}");
                }
            }

            await _db.SaveChangesAsync();

            if (impressos > 0)
            {
                return new JsonResult(new 
                { 
                    success = true, 
                    impresso = impressos,
                    mensagem = erros.Any() 
                        ? $"{impressos} impresso(s), mas houve {erros.Count} erro(s)." 
                        : $"Todas as {impressos} etiqueta(s) foram impressas com sucesso!"
                });
            }
            else
            {
                return new JsonResult(new 
                { 
                    success = false, 
                    error = $"Falha ao imprimir: {string.Join(", ", erros)}" 
                });
            }
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, error = $"Erro: {ex.Message}" });
        }
    }
}

public class PedidoItemExibicao
{
    public int PedidoId { get; set; }
    public int PedidoItemId { get; set; }
    public DateTime DataPedido { get; set; }
    public string PedidoExternoId { get; set; }
    public string NomeCliente { get; set; }
    public string ClienteCpf { get; set; }
    public string Produto { get; set; }
    public string Cor { get; set; }
    public string Tamanho { get; set; }
    public int Quantidade { get; set; }
    public bool Impresso { get; set; }
}

public class AddPedidoModel
{
    [Required(ErrorMessage = "ID Externo é obrigatório")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "ID Externo deve ter até 100 caracteres")]
    public string IdExterno { get; set; }

    [Required(ErrorMessage = "Nome do Cliente é obrigatório")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "Nome deve ter entre 3 e 200 caracteres")]
    public string NomeCliente { get; set; }

    [Required(ErrorMessage = "CPF é obrigatório")]
    [RegularExpression(@"^\d{3}\.\d{3}\.\d{3}-\d{2}$|^\d{11}$", ErrorMessage = "CPF inválido")]
    public string ClienteCpf { get; set; }

    [Required(ErrorMessage = "Data do Pedido é obrigatória")]
    public DateTime DataPedido { get; set; }

    public string? Vendedor { get; set; }

    public string? TipoEnvio { get; set; }

    public string? FormaPagamento { get; set; }

    public decimal ValorFrete { get; set; } = 0;

    public List<AddPedidoItemModel> Itens { get; set; } = new();
}

public class AddPedidoItemModel
{
    public string Produto { get; set; }
    public string SKU { get; set; }
    public string Cor { get; set; }
    public string Tamanho { get; set; }
    public int Quantidade { get; set; } = 1;
    public decimal ValorCusto { get; set; } = 0;
    public decimal ValorVenda { get; set; } = 0;
}