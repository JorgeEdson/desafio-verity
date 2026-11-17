using Verity.Compartilhado.Aplicacao;
using Verity.Compartilhado.Base;
using Verity.Consolidado.Core.Contratos;
using Verity.Consolidado.Core.Dominio.Erros;
using Verity.Consolidado.Core.Dominio.ModelosDeLeitura;
using Verity.Consolidado.Core.Dominio.Servicos;

namespace Verity.Consolidado.Core.UseCases.ObterSaldoDoDia
{
    public sealed class ObterSaldoDoDiaUseCase(
        ISaldoDiarioRepository saldosDiarios,
        IComercianteAtual comercianteAtual)
        : IUseCase<ObterSaldoDoDiaEntrada, SaldoConsolidadoDoDia>
    {
        public async Task<Resultado<SaldoConsolidadoDoDia>> ExecutarAsync(
            ObterSaldoDoDiaEntrada entrada,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(comercianteAtual.Identificador))
                return Resultado<SaldoConsolidadoDoDia>.Falha(CodigosDeErro.ComercianteNaoIdentificado);

            var ultimoSaldoComMovimento = await saldosDiarios.ObterUltimoSaldoComMovimentoAteAsync(entrada.Data, cancellationToken);

            return Resultado<SaldoConsolidadoDoDia>.Sucesso(
                CalculoDoSaldoConsolidado.CalcularDia(entrada.Data, ultimoSaldoComMovimento));
        }
    }
}
