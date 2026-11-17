using Verity.Compartilhado.Aplicacao;
using Verity.Lancamentos.Core.UseCases.Compartilhado;

namespace Verity.Lancamentos.Core.UseCases.EstornarLancamento
{
    public sealed record EstornarLancamentoEntrada(
        Guid IdDoLancamento,
        string ChaveDeIdempotencia) : IRequisicao<RegistroDeLancamentoSaida>;
}
