using Verity.Compartilhado.Hospedagem.Configuracao;
using Verity.Compartilhado.Hospedagem.LimiteDeRequisicoes;
using Verity.Compartilhado.Infraestrutura.Persistencia;
using Verity.Compartilhado.Observabilidade;
using Verity.Compartilhado.Seguranca;
using Verity.Consolidado.Api.Configuracao;
using Verity.Consolidado.Infraestrutura.Configuracao;
using Verity.Consolidado.Infraestrutura.Persistencia;

var builder = WebApplication.CreateBuilder(args);

builder.AdicionarObservabilidade("verity-consolidado-api");
builder.Services.AdicionarInfraestruturaDoConsolidado();
builder.Services.AdicionarCasosDeUsoDeConsulta();
builder.Services.AdicionarAutenticacaoPorJwt(builder.Configuration);
builder.Services.AdicionarAutorizacaoPorComerciante();
builder.Services.AdicionarLimiteDeRequisicoesPorComerciante(builder.Configuration);
builder.Services.AdicionarCacheDoSaldoConsolidado();
builder.Services.AdicionarVerificacaoDeSaudeDoBanco<ConsolidadoDbContext>();
builder.Services.AdicionarRespostasDeErroPadronizadas(StatusHttpDosErrosDoConsolidado.PorCodigoDeErro);
builder.Services.AddControllers();

var app = builder.Build();

if (app.Environment.IsDevelopment())
    await app.AplicarMigracoesAsync<ConsolidadoDbContext>();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.UseOutputCache();

app.MapearHealthChecksDeVivacidadeEProntidao();
app.MapControllers().RequireAuthorization(PoliticasDeAutorizacao.RequerComerciante);

await app.RunAsync();

public partial class Program { }
