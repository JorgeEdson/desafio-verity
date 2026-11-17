using Verity.Consolidado.Core.Dominio.Enumeradores;
using Verity.Consolidado.Core.Dominio.ObjetosDeValor;

namespace Verity.Consolidado.Core.Contratos
{
    public interface IConsolidacaoDeMovimentos
    {
        Task<ResultadoDaConsolidacao> ConsolidarAsync(MovimentoDoLancamento movimento, CancellationToken cancellationToken);
    }
}
