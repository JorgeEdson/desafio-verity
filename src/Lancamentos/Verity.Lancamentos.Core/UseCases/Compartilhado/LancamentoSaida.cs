using Verity.Lancamentos.Core.Dominio.Entidades;

namespace Verity.Lancamentos.Core.UseCases.Compartilhado
{
    public sealed record LancamentoSaida(
        Guid Id,
        string Tipo,
        decimal Valor,
        DateOnly DataCompetencia,
        string Descricao,
        Guid? EstornoDeId,
        DateTime RegistradoEmUtc)
    {
        public static LancamentoSaida APartirDe(Lancamento lancamento) => new(
            lancamento.Id,
            lancamento.Tipo.ToString(),
            lancamento.Valor,
            lancamento.DataCompetencia,
            lancamento.Descricao,
            lancamento.EstornoDeId,
            lancamento.RegistradoEmUtc);
    }
}
