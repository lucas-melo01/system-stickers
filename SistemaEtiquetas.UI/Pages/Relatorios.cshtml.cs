using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using SistemaEtiquetas.Infrastructure.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SistemaEtiquetas.UI.Pages;

public class RelatoriosModel : PageModel
{
    private readonly AppDbContext _db;

    [BindProperty(SupportsGet = true)]
    public DateTime? DataInicio { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? DataFim { get; set; }

    public List<VendaRelatario> Vendas { get; set; } = new();
    public string ErrorMessage { get; set; }
    public string SuccessMessage { get; set; }

    public RelatoriosModel(AppDbContext db)
    {
        _db = db;
    }

    public async Task OnGet()
    {
        // Se houver datas, buscar os pedidos
        if (DataInicio.HasValue && DataFim.HasValue)
        {
            var dataInicio = DateTime.SpecifyKind(DataInicio.Value.Date, DateTimeKind.Utc);
            var dataFim = DateTime.SpecifyKind(DataFim.Value.Date.AddDays(1), DateTimeKind.Utc);

            var pedidos = await _db.Pedidos
                .Include(p => p.Itens)
                .Where(p => p.DataPedido >= dataInicio && p.DataPedido < dataFim)
                .OrderByDescending(p => p.DataPedido)
                .ToListAsync();

            // Montar lista de vendas
            foreach (var pedido in pedidos)
            {
                foreach (var item in pedido.Itens)
                {
                    Vendas.Add(new VendaRelatario
                    {
                        DataPedido = pedido.DataPedido,
                        SKU = item.SKU ?? "N/A",
                        Vendedor = pedido.Vendedor ?? "Manual",
                        Peca = $"{item.Produto} - {item.Cor ?? "N/A"} - {item.Tamanho ?? "N/A"}",
                        Cliente = pedido.NomeCliente,
                        TipoEnvio = pedido.TipoEnvio ?? "N/A",
                        ValorCusto = item.ValorCusto,
                        ValorVenda = item.ValorVenda,
                        FormaPagamento = pedido.FormaPagamento ?? "N/A",
                        ValorFrete = pedido.ValorFrete
                    });
                }
            }
        }
    }

    public async Task<IActionResult> OnPostExportarExcel(DateTime? dataInicio, DateTime? dataFim)
    {
        try
        {
            if (!dataInicio.HasValue || !dataFim.HasValue)
            {
                ErrorMessage = "Selecione um período válido.";
                return RedirectToPage();
            }

            var dataInicioKind = DateTime.SpecifyKind(dataInicio.Value.Date, DateTimeKind.Utc);
            var dataFimKind = DateTime.SpecifyKind(dataFim.Value.Date.AddDays(1), DateTimeKind.Utc);

            var pedidos = await _db.Pedidos
                .Include(p => p.Itens)
                .Where(p => p.DataPedido >= dataInicioKind && p.DataPedido < dataFimKind)
                .OrderByDescending(p => p.DataPedido)
                .ToListAsync();

            if (!pedidos.Any())
            {
                ErrorMessage = "Nenhum pedido encontrado no período selecionado.";
                return RedirectToPage();
            }

            // Licença EPPlus - versão Community
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Vendas");

            // Headers
            var headers = new[] { "Data Pedido", "Código Fornecedor", "Vendedor", "Peça", "Cliente", "Pac/Sedex", "R$ Custo", "R$ Venda", "Forma de Pagamento", "R$ Frete" };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = worksheet.Cells[1, i + 1];
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(0, 22, 35)); // #001623
                cell.Style.Font.Color.SetColor(System.Drawing.Color.FromArgb(255, 242, 0)); // #FFF200
                cell.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
            }

            // Dados
            int row = 2;
            decimal totalCusto = 0;
            decimal totalVenda = 0;
            decimal totalFrete = 0;

            foreach (var pedido in pedidos)
            {
                foreach (var item in pedido.Itens)
                {
                    worksheet.Cells[row, 1].Value = pedido.DataPedido.ToString("dd/MM/yyyy");
                    worksheet.Cells[row, 2].Value = item.SKU ?? "N/A";
                    worksheet.Cells[row, 3].Value = pedido.Vendedor ?? "Manual";
                    worksheet.Cells[row, 4].Value = $"{item.Produto} - {item.Cor ?? "N/A"} - {item.Tamanho ?? "N/A"}";
                    worksheet.Cells[row, 5].Value = pedido.NomeCliente;
                    worksheet.Cells[row, 6].Value = pedido.TipoEnvio ?? "N/A";
                    worksheet.Cells[row, 7].Value = item.ValorCusto;
                    worksheet.Cells[row, 8].Value = item.ValorVenda;
                    worksheet.Cells[row, 9].Value = pedido.FormaPagamento ?? "N/A";
                    worksheet.Cells[row, 10].Value = pedido.ValorFrete;

                    // Formatar como moeda
                    worksheet.Cells[row, 7].Style.Numberformat.Format = "R$ #,##0.00";
                    worksheet.Cells[row, 8].Style.Numberformat.Format = "R$ #,##0.00";
                    worksheet.Cells[row, 10].Style.Numberformat.Format = "R$ #,##0.00";

                    totalCusto += item.ValorCusto;
                    totalVenda += item.ValorVenda;
                    totalFrete += pedido.ValorFrete;

                    row++;
                }
            }

            // Totalizadores
            row++;
            worksheet.Cells[row, 6].Value = "TOTAL:";
            worksheet.Cells[row, 6].Style.Font.Bold = true;
            worksheet.Cells[row, 6].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;

            worksheet.Cells[row, 7].Value = totalCusto;
            worksheet.Cells[row, 7].Style.Font.Bold = true;
            worksheet.Cells[row, 7].Style.Numberformat.Format = "R$ #,##0.00";

            worksheet.Cells[row, 8].Value = totalVenda;
            worksheet.Cells[row, 8].Style.Font.Bold = true;
            worksheet.Cells[row, 8].Style.Numberformat.Format = "R$ #,##0.00";

            worksheet.Cells[row, 10].Value = totalFrete;
            worksheet.Cells[row, 10].Style.Font.Bold = true;
            worksheet.Cells[row, 10].Style.Numberformat.Format = "R$ #,##0.00";

            // Ajustar largura das colunas
            for (int i = 1; i <= headers.Length; i++)
            {
                worksheet.Column(i).AutoFit();
            }

            // Gerar arquivo
            var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;

            string fileName = $"Relatorio_Vendas_{dataInicio:dd-MM-yyyy}_a_{dataFim:dd-MM-yyyy}.xlsx";
            return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Erro ao exportar relatório: {ex.Message}";
            return RedirectToPage();
        }
    }
}

public class VendaRelatario
{
    public DateTime DataPedido { get; set; }
    public string SKU { get; set; }
    public string Vendedor { get; set; }
    public string Peca { get; set; }
    public string Cliente { get; set; }
    public string TipoEnvio { get; set; }
    public decimal ValorCusto { get; set; }
    public decimal ValorVenda { get; set; }
    public string FormaPagamento { get; set; }
    public decimal ValorFrete { get; set; }
}
