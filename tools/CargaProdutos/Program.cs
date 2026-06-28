using System.Globalization;
using Microsoft.EntityFrameworkCore;
using SistemaEtiquetas.Domain;
using SistemaEtiquetas.Domain.Entities;
using SistemaEtiquetas.Infrastructure.Data;

if (args.Length == 0 || args.Contains("-h") || args.Contains("--help"))
{
    Console.WriteLine("""
    Uso: dotnet run --project tools/CargaProdutos -- <ficheiro.csv> [--dry-run]

    Colunas esperadas (cabeçalho, separador ; ou ,):
      Loja, ProdutoIdLojaIntegrada, Nome, Sku, CodigoFornecedor, FornecedorId ou NomeFornecedor

    Loja: Resume Modas | DonnaKora | ResumeModas
    """);
    return args.Length == 0 ? 1 : 0;
}

var csvPath = args[0];
var dryRun = args.Contains("--dry-run");

if (!File.Exists(csvPath))
{
    Console.Error.WriteLine($"Ficheiro não encontrado: {csvPath}");
    return 1;
}

var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
if (string.IsNullOrWhiteSpace(databaseUrl))
{
    Console.Error.WriteLine("Defina DATABASE_URL.");
    return 1;
}

await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(databaseUrl).Options);

var linhas = File.ReadAllLines(csvPath);
if (linhas.Length < 2)
{
    Console.Error.WriteLine("CSV vazio ou sem dados.");
    return 1;
}

var sep = linhas[0].Contains(';') ? ';' : ',';
var headers = linhas[0].Split(sep).Select(h => h.Trim().ToLowerInvariant()).ToArray();

int Col(string name) => Array.FindIndex(headers, h => h == name.ToLowerInvariant());

var iLoja = Col("loja");
var iIdLi = Col("produtoidlojaintegrada");
var iNome = Col("nome");
var iSku = Col("sku");
var iCod = Col("codigofornecedor");
var iFornId = Col("fornecedorid");
var iFornNome = Col("nomefornecedor");

if (iLoja < 0 || iIdLi < 0 || iNome < 0)
{
    Console.Error.WriteLine("Colunas obrigatórias: Loja, ProdutoIdLojaIntegrada, Nome");
    return 1;
}

var inseridos = 0;
var atualizados = 0;
var ignorados = 0;

for (var row = 1; row < linhas.Length; row++)
{
    var line = linhas[row].Trim();
    if (line.Length == 0) continue;
    var cols = line.Split(sep);
    string Get(int i) => i >= 0 && i < cols.Length ? cols[i].Trim() : "";

    var loja = LojaOrigemHelper.Parse(Get(iLoja));
    if (!loja.HasValue)
    {
        Console.WriteLine($"[IGNORADO] Linha {row + 1}: loja inválida");
        ignorados++;
        continue;
    }

    if (!long.TryParse(Get(iIdLi), out var idLi) || idLi <= 0)
    {
        Console.WriteLine($"[IGNORADO] Linha {row + 1}: ProdutoIdLojaIntegrada inválido");
        ignorados++;
        continue;
    }

    var nome = Get(iNome);
    if (string.IsNullOrWhiteSpace(nome))
    {
        ignorados++;
        continue;
    }

    int? fornecedorId = null;
    if (iFornId >= 0 && int.TryParse(Get(iFornId), out var fid) && fid > 0)
        fornecedorId = fid;
    else if (iFornNome >= 0)
    {
        var fn = Get(iFornNome);
        if (!string.IsNullOrWhiteSpace(fn))
        {
            var f = await db.Fornecedores.FirstOrDefaultAsync(x => x.NomeRazaoSocial.ToLower() == fn.ToLower());
            if (f != null) fornecedorId = f.Id;
            else Console.WriteLine($"[AVISO] Linha {row + 1}: fornecedor '{fn}' não encontrado");
        }
    }

    var existente = await db.Produtos.FirstOrDefaultAsync(p => p.Loja == loja && p.ProdutoIdLojaIntegrada == idLi);
    if (existente == null)
    {
        db.Produtos.Add(new Produto
        {
            Loja = loja.Value,
            ProdutoIdLojaIntegrada = idLi,
            Nome = nome,
            Sku = NullIfEmpty(Get(iSku)),
            CodigoFornecedor = NullIfEmpty(Get(iCod)),
            FornecedorId = fornecedorId,
            Ativo = true,
            CriadoEm = DateTime.UtcNow,
            AtualizadoEm = DateTime.UtcNow,
        });
        inseridos++;
    }
    else
    {
        existente.Nome = nome;
        existente.Sku = NullIfEmpty(Get(iSku)) ?? existente.Sku;
        existente.CodigoFornecedor = NullIfEmpty(Get(iCod)) ?? existente.CodigoFornecedor;
        if (fornecedorId.HasValue) existente.FornecedorId = fornecedorId;
        existente.AtualizadoEm = DateTime.UtcNow;
        atualizados++;
    }
}

if (dryRun)
{
    Console.WriteLine($"--dry-run: {inseridos} inserções, {atualizados} atualizações, {ignorados} ignoradas (não gravado).");
    return 0;
}

await db.SaveChangesAsync();
Console.WriteLine($"Concluído: {inseridos} inseridos, {atualizados} atualizados, {ignorados} ignorados.");
return 0;

static string? NullIfEmpty(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
