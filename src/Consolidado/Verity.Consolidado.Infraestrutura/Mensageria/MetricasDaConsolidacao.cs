using System.Diagnostics.Metrics;
using Verity.Consolidado.Core.Dominio.Enumeradores;

namespace Verity.Consolidado.Infraestrutura.Mensageria
{
    public sealed class MetricasDaConsolidacao
    {
        public const string NomeDoMedidor = "Verity.Consolidado.Consumo";

        private const string EtiquetaDoResultado = "resultado";
        private const string ResultadoConsolidado = "consolidado";
        private const string ResultadoDuplicado = "duplicado";
        private const string ResultadoRejeitado = "rejeitado";

        private readonly TimeProvider _provedorDeTempo;
        private readonly Counter<long> _eventosProcessados;
        private readonly Histogram<double> _duracaoDoProcessamento;
        private readonly Histogram<double> _atrasoDesdeORegistro;

        public MetricasDaConsolidacao(IMeterFactory fabricaDeMedidores, TimeProvider provedorDeTempo)
        {
            _provedorDeTempo = provedorDeTempo;

            var medidor = fabricaDeMedidores.Create(NomeDoMedidor);

            _eventosProcessados = medidor.CreateCounter<long>(
                "verity.consolidacao.eventos_processados",
                unit: "{evento}",
                description: "Eventos de lançamento processados pelo worker, por resultado.");

            _duracaoDoProcessamento = medidor.CreateHistogram<double>(
                "verity.consolidacao.duracao",
                unit: "s",
                description: "Tempo para consolidar um evento, incluindo novas tentativas.");

            _atrasoDesdeORegistro = medidor.CreateHistogram<double>(
                "verity.consolidacao.atraso_desde_o_registro",
                unit: "s",
                description: "Tempo entre o registro do lançamento e a atualização do saldo consolidado.");
        }

        public void RegistrarEventoAceito(ResultadoDaConsolidacao resultado, DateTime registradoEmUtc, TimeSpan duracao)
        {
            var nomeDoResultado = resultado == ResultadoDaConsolidacao.MovimentoConsolidado ? ResultadoConsolidado : ResultadoDuplicado;
            RegistrarProcessamento(nomeDoResultado, duracao);

            if (resultado == ResultadoDaConsolidacao.MovimentoConsolidado)
                _atrasoDesdeORegistro.Record(Math.Max(0, (_provedorDeTempo.GetUtcNow().UtcDateTime - registradoEmUtc).TotalSeconds));
        }

        public void RegistrarEventoRejeitado(TimeSpan duracao) => RegistrarProcessamento(ResultadoRejeitado, duracao);

        private void RegistrarProcessamento(string nomeDoResultado, TimeSpan duracao)
        {
            var etiqueta = new KeyValuePair<string, object?>(EtiquetaDoResultado, nomeDoResultado);
            _eventosProcessados.Add(1, etiqueta);
            _duracaoDoProcessamento.Record(duracao.TotalSeconds, etiqueta);
        }
    }
}
