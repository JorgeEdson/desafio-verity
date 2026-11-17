using Verity.Compartilhado.Integracao;

namespace Verity.Consolidado.Testes.Compartilhado
{
    public sealed class LancamentoRegistradoBuilder
    {
        private Guid? _eventoIdInformado;
        private int _versao = LancamentoRegistradoIntegrationEvent.VersaoAtual;
        private string _comercianteId = "c-teste";
        private string _tipo = LancamentoRegistradoIntegrationEvent.TipoCredito;
        private decimal _valor = 100.00m;
        private DateOnly _dataCompetencia = new(2026, 11, 10);

        public LancamentoRegistradoBuilder ComEventoId(Guid eventoId)
        {
            _eventoIdInformado = eventoId;
            return this;
        }

        public LancamentoRegistradoBuilder NaVersao(int versao)
        {
            _versao = versao;
            return this;
        }

        public LancamentoRegistradoBuilder DoComerciante(string comercianteId)
        {
            _comercianteId = comercianteId;
            return this;
        }

        public LancamentoRegistradoBuilder Credito(decimal valor)
        {
            _tipo = LancamentoRegistradoIntegrationEvent.TipoCredito;
            _valor = valor;
            return this;
        }

        public LancamentoRegistradoBuilder Debito(decimal valor)
        {
            _tipo = LancamentoRegistradoIntegrationEvent.TipoDebito;
            _valor = valor;
            return this;
        }

        public LancamentoRegistradoBuilder DoTipo(string tipo)
        {
            _tipo = tipo;
            return this;
        }

        public LancamentoRegistradoBuilder NaData(DateOnly dataCompetencia)
        {
            _dataCompetencia = dataCompetencia;
            return this;
        }

        public LancamentoRegistradoIntegrationEvent Construir() => new(
            _eventoIdInformado ?? Guid.CreateVersion7(),
            _versao,
            DateTime.UtcNow,
            Guid.CreateVersion7(),
            _comercianteId,
            _tipo,
            _valor,
            _dataCompetencia);
    }
}
