using OfficeOpenXml;
using OfficeOpenXml.Style;
using SistemaEtiquetas.Domain;
using SistemaEtiquetas.Domain.Entities;

namespace SistemaEtiquetas.API.Services;

public static class RelatorioExcelService
{
    public static byte[] GerarVendasExcel(IReadOnlyList<Pedido> pedidos, DateTime inicio, DateTime fim)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Vendas");

        var headers = new[] { "Data Pedido", "Código Fornecedor", "Vendedor", "Peça", "Cliente", "Pac/Sedex", "R$ Custo", "R$ Venda", "Forma de Pagamento", "R$ Frete" };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = worksheet.Cells[1, i + 1];
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
            cell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(0, 22, 35));
            cell.Style.Font.Color.SetColor(System.Drawing.Color.FromArgb(255, 242, 0));
            cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        }

        int row = 2;
        decimal totalCusto = 0;
        decimal totalVenda = 0;
        decimal totalFrete = 0;

        foreach (var pedido in pedidos)
        {
            foreach (var item in pedido.Itens.OrderBy(x => x.Id))
            {
                worksheet.Cells[row, 1].Value = TimeZoneBrasil.DeUtcParaBrasilia(pedido.DataPedido).ToString("dd/MM/yyyy HH:mm");
                worksheet.Cells[row, 2].Value = item.SKU ?? "N/A";
                worksheet.Cells[row, 3].Value = pedido.Vendedor ?? "Manual";
                worksheet.Cells[row, 4].Value = $"{item.Produto} - {item.Cor ?? "N/A"} - {item.Tamanho ?? "N/A"}";
                worksheet.Cells[row, 5].Value = pedido.NomeCliente;
                worksheet.Cells[row, 6].Value = pedido.TipoEnvio ?? "N/A";
                worksheet.Cells[row, 7].Value = item.ValorCusto;
                worksheet.Cells[row, 8].Value = item.ValorVenda;
                worksheet.Cells[row, 9].Value = pedido.FormaPagamento ?? "N/A";
                worksheet.Cells[row, 10].Value = pedido.ValorFrete;

                worksheet.Cells[row, 7].Style.Numberformat.Format = "R$ #,##0.00";
                worksheet.Cells[row, 8].Style.Numberformat.Format = "R$ #,##0.00";
                worksheet.Cells[row, 10].Style.Numberformat.Format = "R$ #,##0.00";

                totalCusto += item.ValorCusto;
                totalVenda += item.ValorVenda;
                totalFrete += pedido.ValorFrete;
                row++;
            }
        }

        row++;
        worksheet.Cells[row, 6].Value = "TOTAL:";
        worksheet.Cells[row, 6].Style.Font.Bold = true;
        worksheet.Cells[row, 6].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

        worksheet.Cells[row, 7].Value = totalCusto;
        worksheet.Cells[row, 7].Style.Font.Bold = true;
        worksheet.Cells[row, 7].Style.Numberformat.Format = "R$ #,##0.00";

        worksheet.Cells[row, 8].Value = totalVenda;
        worksheet.Cells[row, 8].Style.Font.Bold = true;
        worksheet.Cells[row, 8].Style.Numberformat.Format = "R$ #,##0.00";

        worksheet.Cells[row, 10].Value = totalFrete;
        worksheet.Cells[row, 10].Style.Font.Bold = true;
        worksheet.Cells[row, 10].Style.Numberformat.Format = "R$ #,##0.00";

        for (int i = 1; i <= headers.Length; i++)
            worksheet.Column(i).AutoFit();

        return package.GetAsByteArray();
    }
}
