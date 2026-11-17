using Verity.Compartilhado.Aplicacao;
using Verity.Lancamentos.Core.UseCases.Compartilhado;

namespace Verity.Lancamentos.Core.UseCases.ObterLancamentoPorId
{
    public sealed record ObterLancamentoPorIdEntrada(Guid Id) : IRequisicao<LancamentoSaida>;
}
