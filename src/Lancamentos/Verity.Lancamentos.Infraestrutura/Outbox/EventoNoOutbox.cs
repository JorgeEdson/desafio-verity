namespace Verity.Lancamentos.Infraestrutura.Outbox
{
    public sealed class EventoNoOutbox
    {
        public const int TamanhoMaximoDaMensagemDeErro = 2000;

        private EventoNoOutbox()
        {
            TipoDoEvento = string.Empty;
            ChaveDeRoteamento = string.Empty;
            Conteudo = string.Empty;
        }

        public long Id { get; private set; }

        public Guid EventoId { get; private set; }

        public string TipoDoEvento { get; private set; }

        public string ChaveDeRoteamento { get; private set; }

        public string Conteudo { get; private set; }

        public string? ContextoDeRastreamento { get; private set; }

        public DateTime OcorridoEmUtc { get; private set; }

        public DateTime? PublicadoEmUtc { get; private set; }

        public int Tentativas { get; private set; }

        public string? UltimoErro { get; private set; }

        public static EventoNoOutbox Criar(
            Guid eventoId,
            string tipoDoEvento,
            string chaveDeRoteamento,
            string conteudo,
            string? contextoDeRastreamento,
            DateTime ocorridoEmUtc) => new()
            {
                EventoId = eventoId,
                TipoDoEvento = tipoDoEvento,
                ChaveDeRoteamento = chaveDeRoteamento,
                Conteudo = conteudo,
                ContextoDeRastreamento = contextoDeRastreamento,
                OcorridoEmUtc = ocorridoEmUtc
            };

        public void MarcarComoPublicado(DateTime publicadoEmUtc)
        {
            PublicadoEmUtc = publicadoEmUtc;
            UltimoErro = null;
        }

        public void RegistrarFalhaDePublicacao(string mensagemDeErro)
        {
            Tentativas++;
            UltimoErro = mensagemDeErro.Length <= TamanhoMaximoDaMensagemDeErro
                ? mensagemDeErro
                : mensagemDeErro[..TamanhoMaximoDaMensagemDeErro];
        }
    }
}
