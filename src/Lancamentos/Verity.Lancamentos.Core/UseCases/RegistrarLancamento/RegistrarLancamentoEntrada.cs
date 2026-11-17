using Verity.Compartilhado.Aplicacao;
using Verity.Lancamentos.Core.UseCases.Compartilhado;

namespace Verity.Lancamentos.Core.UseCases.RegistrarLancamento
{
    public sealed record RegistrarLancamentoEntrada(
        string Tipo,
        decimal Valor,
        DateOnly DataCompetencia,
        string Descricao,
        string ChaveDeIdempotencia) : IRequisicao<RegistroDeLancamentoSaida>;
}
