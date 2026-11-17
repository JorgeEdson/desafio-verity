using Verity.Compartilhado.Hospedagem.Configuracao;
using Verity.Compartilhado.Observabilidade;
using Verity.Gateway.Configuracao;

var builder = WebApplication.CreateBuilder(args);

builder.AdicionarObservabilidade("verity-gateway");
builder.WebHost.ConfigurarServidorKestrel();
builder.Services.AdicionarProxyReverso(builder.Configuration);
builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapearHealthChecksDeVivacidadeEProntidao();
app.MapReverseProxy();

await app.RunAsync();

public partial class Program { }
