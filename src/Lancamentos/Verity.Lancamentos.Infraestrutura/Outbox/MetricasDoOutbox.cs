using System.Diagnostics.Metrics;

namespace Verity.Lancamentos.Infraestrutura.Outbox
{
    public sealed class MetricasDoOutbox
    {
        public const string NomeDoMedidor = "Verity.Lancamentos.Outbox";

        private readonly TimeProvider _provedorDeTempo;
        private readonly Counter<long> _eventosPublicados;
        private SituacaoDoOutbox _ultimaSituacaoMedida = new(0, null);

        public MetricasDoOutbox(IMeterFactory fabricaDeMedidores, TimeProvider provedorDeTempo)
        {
            _provedorDeTempo = provedorDeTempo;

            var medidor = fabricaDeMedidores.Create(NomeDoMedidor);

            _eventosPublicados = medidor.CreateCounter<long>(
                "verity.outbox.eventos_publicados",
                unit: "{evento}",
                description: "Eventos do outbox publicados no RabbitMQ.");

            medidor.CreateObservableGauge(
                "verity.outbox.eventos_pendentes",
                () => Volatile.Read(ref _ultimaSituacaoMedida).EventosPendentes,
                unit: "{evento}",
                description: "Eventos gravados no outbox e ainda não publicados.");

            medidor.CreateObservableGauge(
                "verity.outbox.idade_do_evento_pendente_mais_antigo",
                CalcularIdadeDoEventoPendenteMaisAntigoEmSegundos,
                unit: "s",
                description: "Há quanto tempo o evento pendente mais antigo aguarda publicação.");
        }

        public void RegistrarEventosPublicados(int quantidadeDeEventos) => _eventosPublicados.Add(quantidadeDeEventos);

        public void AtualizarSituacao(SituacaoDoOutbox situacao) => Volatile.Write(ref _ultimaSituacaoMedida, situacao);

        private double CalcularIdadeDoEventoPendenteMaisAntigoEmSegundos()
        {
            var ocorrenciaDoMaisAntigo = Volatile.Read(ref _ultimaSituacaoMedida).OcorrenciaDoEventoPendenteMaisAntigoEmUtc;

            return ocorrenciaDoMaisAntigo is null
                ? 0
                : Math.Max(0, (_provedorDeTempo.GetUtcNow().UtcDateTime - ocorrenciaDoMaisAntigo.Value).TotalSeconds);
        }
    }
}
