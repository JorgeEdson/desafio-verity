using System.Diagnostics;
using Verity.Compartilhado.Base;
using Verity.Compartilhado.Integracao;
using Verity.Lancamentos.Core.Dominio.Eventos;

namespace Verity.Lancamentos.Infraestrutura.Outbox
{
    public static class TradutorDeEventosDeDominioParaIntegracao
    {
        public static EventoNoOutbox Traduzir(EventoDeDominio eventoDeDominio) => eventoDeDominio switch
        {
            LancamentoRegistrado lancamentoRegistrado => TraduzirLancamentoRegistrado(lancamentoRegistrado),
            _ => throw new InvalidOperationException(
                $"O evento de domínio '{eventoDeDominio.GetType().Name}' não possui tradução para evento de integração.")
        };

        private static EventoNoOutbox TraduzirLancamentoRegistrado(LancamentoRegistrado lancamentoRegistrado)
        {
            var eventoDeIntegracao = new LancamentoRegistradoIntegrationEvent(
                lancamentoRegistrado.IdDoEvento,
                LancamentoRegistradoIntegrationEvent.VersaoAtual,
                lancamentoRegistrado.OcorridoEmUtc,
                lancamentoRegistrado.IdDoAgregado,
                lancamentoRegistrado.ComercianteId,
                lancamentoRegistrado.Tipo.ToString(),
                lancamentoRegistrado.Valor,
                lancamentoRegistrado.DataCompetencia);

            return EventoNoOutbox.Criar(
                lancamentoRegistrado.IdDoEvento,
                LancamentoRegistradoIntegrationEvent.NomeDoEvento,
                TopologiaDeMensageria.ChaveDeRoteamentoDeLancamentoRegistrado,
                SerializacaoDeEventosDeIntegracao.Serializar(eventoDeIntegracao),
                Activity.Current?.Id,
                lancamentoRegistrado.OcorridoEmUtc);
        }
    }
}
