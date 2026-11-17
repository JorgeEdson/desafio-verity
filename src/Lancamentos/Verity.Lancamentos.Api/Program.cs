using Verity.Compartilhado.Hospedagem.Configuracao;
using Verity.Compartilhado.Hospedagem.LimiteDeRequisicoes;
using Verity.Compartilhado.Infraestrutura.Persistencia;
using Verity.Compartilhado.Observabilidade;
using Verity.Compartilhado.Seguranca;
using Verity.Lancamentos.Api.Configuracao;
using Verity.Lancamentos.Infraestrutura.Configuracao;
using Verity.Lancamentos.Infraestrutura.Persistencia;

var builder = WebApplication.CreateBuilder(args);

builder.AdicionarObservabilidade("verity-lancamentos-api");
builder.Services.AdicionarInfraestruturaDeLancamentos(builder.Configuration);
builder.Services.AdicionarCasosDeUsoDeLancamentos();
builder.Services.AdicionarAutenticacaoPorJwt(builder.Configuration);
builder.Services.AdicionarAutorizacaoPorComerciante();
builder.Services.AdicionarLimiteDeRequisicoesPorComerciante(builder.Configuration);
builder.Services.AdicionarVerificacaoDeSaudeDoBanco<LancamentosDbContext>();
builder.Services.AdicionarRespostasDeErroPadronizadas(StatusHttpDosErrosDeLancamentos.PorCodigoDeErro);
builder.Services.AddControllers();

var app = builder.Build();

if (app.Environment.IsDevelopment())
    await app.AplicarMigracoesAsync<LancamentosDbContext>();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapearHealthChecksDeVivacidadeEProntidao();
app.MapControllers().RequireAuthorization(PoliticasDeAutorizacao.RequerComerciante);

await app.RunAsync();

public partial class Program { }
