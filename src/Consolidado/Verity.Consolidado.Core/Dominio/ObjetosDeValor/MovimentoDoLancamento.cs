using Verity.Compartilhado.Base;
using Verity.Compartilhado.Integracao;
using Verity.Consolidado.Core.Dominio.Erros;

namespace Verity.Consolidado.Core.Dominio.ObjetosDeValor
{
    public sealed class MovimentoDoLancamento
    {
        private MovimentoDoLancamento(
            Guid eventoId,
            string comercianteId,
            DateOnly dataCompetencia,
            decimal totalDeCreditos,
            decimal totalDeDebitos)
        {
            EventoId = eventoId;
            ComercianteId = comercianteId;
            DataCompetencia = dataCompetencia;
            TotalDeCreditos = totalDeCreditos;
            TotalDeDebitos = totalDeDebitos;
        }

        public Guid EventoId { get; }

        public string ComercianteId { get; }

        public DateOnly DataCompetencia { get; }

        public decimal TotalDeCreditos { get; }

        public decimal TotalDeDebitos { get; }

        public decimal VariacaoDoSaldo => TotalDeCreditos - TotalDeDebitos;

        public static Resultado<MovimentoDoLancamento> TentarCriar(LancamentoRegistradoIntegrationEvent lancamentoRegistrado)
        {
            var ehCredito = lancamentoRegistrado.Tipo == LancamentoRegistradoIntegrationEvent.TipoCredito;
            var ehDebito = lancamentoRegistrado.Tipo == LancamentoRegistradoIntegrationEvent.TipoDebito;

            var erros = new List<string>();

            if (lancamentoRegistrado.Versao != LancamentoRegistradoIntegrationEvent.VersaoAtual)
                erros.Add(CodigosDeErro.EventoComVersaoNaoSuportada);

            if (lancamentoRegistrado.EventoId == Guid.Empty)
                erros.Add(CodigosDeErro.EventoSemIdentificador);

            if (string.IsNullOrWhiteSpace(lancamentoRegistrado.ComercianteId))
                erros.Add(CodigosDeErro.EventoSemComerciante);

            if (lancamentoRegistrado.Valor <= 0m)
                erros.Add(CodigosDeErro.EventoComValorInvalido);

            if (!ehCredito && !ehDebito)
                erros.Add(CodigosDeErro.EventoComTipoDeLancamentoInvalido);

            if (erros.Count > 0)
                return Resultado<MovimentoDoLancamento>.Falha(erros);

            return Resultado<MovimentoDoLancamento>.Sucesso(new MovimentoDoLancamento(
                lancamentoRegistrado.EventoId,
                lancamentoRegistrado.ComercianteId,
                lancamentoRegistrado.DataCompetencia,
                totalDeCreditos: ehCredito ? lancamentoRegistrado.Valor : 0m,
                totalDeDebitos: ehDebito ? lancamentoRegistrado.Valor : 0m));
        }
    }
}
