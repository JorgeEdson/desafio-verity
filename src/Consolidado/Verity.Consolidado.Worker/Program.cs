using Verity.Compartilhado.Observabilidade;
using Verity.Consolidado.Infraestrutura.Configuracao;

var builder = Host.CreateApplicationBuilder(args);

builder.AdicionarObservabilidade("verity-consolidado-worker");
builder.Services.AdicionarInfraestruturaDoConsolidado();
builder.Services.AdicionarConsumoDeLancamentosRegistrados(builder.Configuration);

var host = builder.Build();

await host.RunAsync();
