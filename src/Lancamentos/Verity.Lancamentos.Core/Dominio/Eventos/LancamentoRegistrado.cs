using Verity.Compartilhado.Base;
using Verity.Lancamentos.Core.Dominio.Entidades;
using Verity.Lancamentos.Core.Dominio.Enumeradores;

namespace Verity.Lancamentos.Core.Dominio.Eventos
{
    public sealed class LancamentoRegistrado : EventoDeDominio
    {
        public LancamentoRegistrado(Lancamento lancamento)
            : base(lancamento.Id, lancamento.RegistradoEmUtc)
        {
            ComercianteId = lancamento.ComercianteId;
            Tipo = lancamento.Tipo;
            Valor = lancamento.Valor;
            DataCompetencia = lancamento.DataCompetencia;
        }

        public string ComercianteId { get; }

        public TipoDeLancamento Tipo { get; }

        public decimal Valor { get; }

        public DateOnly DataCompetencia { get; }
    }
}
