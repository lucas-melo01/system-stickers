using Microsoft.EntityFrameworkCore;
using SistemaEtiquetas.Domain;
using SistemaEtiquetas.Domain.Entities;
using SistemaEtiquetas.Infrastructure.Data;

namespace SistemaEtiquetas.API.Services;

public sealed class EncomendaNotificacaoService
{
    private readonly AppDbContext _db;
    private readonly LojaIntegradaProdutoApi _catalogo;
    private readonly WhatsAppCloudApiService _whatsApp;
    private readonly ILogger<EncomendaNotificacaoService> _logger;

    public EncomendaNotificacaoService(
        AppDbContext db,
        LojaIntegradaProdutoApi catalogo,
        WhatsAppCloudApiService whatsApp,
        ILogger<EncomendaNotificacaoService> logger)
    {
        _db = db;
        _catalogo = catalogo;
        _whatsApp = whatsApp;
        _logger = logger;
    }

    public async Task ProcessarEncomendasAsync(
        Pedido pedido,
        LojaOrigem loja,
        string vendedor,
        IReadOnlyList<(PedidoItem item, long produtoIdLojaIntegrada)> itensEncomenda,
        CancellationToken cancellationToken = default)
    {
        foreach (var (item, produtoIdLi) in itensEncomenda)
        {
            await ProcessarItemAsync(pedido, item, loja, vendedor, produtoIdLi, cancellationToken);
        }
    }

    private async Task ProcessarItemAsync(
        Pedido pedido,
        PedidoItem item,
        LojaOrigem loja,
        string vendedor,
        long produtoIdLi,
        CancellationToken cancellationToken)
    {
        var produto = await _db.Produtos.AsNoTracking()
            .Include(p => p.Fornecedor)
            .FirstOrDefaultAsync(p => p.Loja == loja && p.ProdutoIdLojaIntegrada == produtoIdLi, cancellationToken);

        var cor = item.Cor ?? "-";
        var tamanho = item.Tamanho ?? "-";
        var codigo = item.SKU;
        string? avisoImagem = null;
        string? imagemUrl = null;

        if (produtoIdLi > 0)
        {
            var detalhes = await _catalogo.ObterDetalhesProdutoAsync(produtoIdLi, vendedor, cancellationToken);
            if (detalhes?.ImagemUrl != null)
                imagemUrl = detalhes.ImagemUrl;
            else
                avisoImagem = "Imagem indisponível na Loja Integrada.";
            if (!string.IsNullOrWhiteSpace(detalhes?.Mpn))
                codigo = detalhes.Mpn;
        }

        if (string.IsNullOrWhiteSpace(codigo) && produto != null)
            codigo = produto.CodigoFornecedor ?? produto.Sku ?? "-";

        var mensagem = MontarMensagem(item.Produto, cor, tamanho, codigo ?? "-", pedido.NomeCliente, pedido.PedidoExternoId);

        if (produto == null)
        {
            await RegistrarNotificacaoAsync(pedido, item, loja, null, null, mensagem, StatusNotificacaoFornecedor.Falha,
                null, "Produto não encontrado no catálogo.", cancellationToken);
            return;
        }

        if (produto.FornecedorId == null || produto.Fornecedor == null)
        {
            await RegistrarNotificacaoAsync(pedido, item, loja, produto.Id, null, mensagem, StatusNotificacaoFornecedor.Falha,
                null, "Fornecedor não vinculado.", cancellationToken);
            return;
        }

        var fornecedor = produto.Fornecedor;
        if (string.IsNullOrWhiteSpace(fornecedor.WhatsApp) || fornecedor.WhatsApp.Length < 10)
        {
            await RegistrarNotificacaoAsync(pedido, item, loja, produto.Id, fornecedor.Id, mensagem, StatusNotificacaoFornecedor.Falha,
                null, "WhatsApp do fornecedor inválido.", cancellationToken);
            return;
        }

        var (ok, messageId, erro) = await _whatsApp.EnviarTemplateEncomendaAsync(
            fornecedor.WhatsApp,
            imagemUrl,
            item.Produto,
            cor,
            tamanho,
            codigo ?? "-",
            pedido.NomeCliente,
            pedido.PedidoExternoId,
            cancellationToken);

        var msgFinal = avisoImagem != null ? $"{mensagem}\n\n[Aviso: {avisoImagem}]" : mensagem;

        if (ok)
        {
            await RegistrarNotificacaoAsync(pedido, item, loja, produto.Id, fornecedor.Id, msgFinal,
                StatusNotificacaoFornecedor.Enviado, messageId, null, cancellationToken);
        }
        else
        {
            await RegistrarNotificacaoAsync(pedido, item, loja, produto.Id, fornecedor.Id, msgFinal,
                StatusNotificacaoFornecedor.Falha, null, erro, cancellationToken);
        }
    }

    private static string MontarMensagem(
        string nomeProduto, string cor, string tamanho, string codigo,
        string nomeCliente, string pedidoExternoId) =>
        $"{nomeProduto}\nCor: {cor}\nTamanho: {tamanho}\nCódigo Fornecedor: {codigo}\nCliente: {nomeCliente} - Pedido: {pedidoExternoId}";

    private async Task RegistrarNotificacaoAsync(
        Pedido pedido,
        PedidoItem item,
        LojaOrigem loja,
        int? produtoId,
        int? fornecedorId,
        string mensagem,
        StatusNotificacaoFornecedor status,
        string? whatsAppMessageId,
        string? erro,
        CancellationToken cancellationToken)
    {
        var n = new NotificacaoFornecedor
        {
            PedidoId = pedido.Id,
            PedidoItemId = item.Id,
            FornecedorId = fornecedorId,
            ProdutoId = produtoId,
            Loja = loja,
            PedidoExternoId = pedido.PedidoExternoId,
            NomeCliente = pedido.NomeCliente,
            Status = status,
            MensagemTexto = mensagem,
            WhatsAppMessageId = whatsAppMessageId,
            Erro = erro,
            CriadoEm = DateTime.UtcNow,
            EnviadoEm = status == StatusNotificacaoFornecedor.Enviado ? DateTime.UtcNow : null,
        };
        _db.NotificacoesFornecedor.Add(n);
        await _db.SaveChangesAsync(cancellationToken);

        if (status == StatusNotificacaoFornecedor.Falha)
            _logger.LogWarning("Notificação encomenda falhou pedido {Pedido} item {Item}: {Erro}",
                pedido.PedidoExternoId, item.Id, erro);
    }
}
