using System.Diagnostics;
using System.Text;

namespace Verity.Compartilhado.Observabilidade
{
    public static class RastreamentoDeMensageria
    {
        public const string NomeDaFonteDeAtividades = "Verity.Mensageria";
        public const string CabecalhoDeContextoDeRastreamento = "traceparent";

        private const string SistemaDeMensageria = "rabbitmq";
        private const string OperacaoDeEnvio = "send";
        private const string OperacaoDeProcessamento = "process";

        private static readonly ActivitySource FonteDeAtividades = new(NomeDaFonteDeAtividades);

        public static Activity? IniciarPublicacao(string destino, string idDaMensagem, string? contextoDeRastreamentoDaOrigem) =>
            Iniciar($"{destino} {OperacaoDeEnvio}", ActivityKind.Producer, contextoDeRastreamentoDaOrigem, destino, idDaMensagem, OperacaoDeEnvio);

        public static Activity? IniciarProcessamento(string destino, string? idDaMensagem, IDictionary<string, object?>? cabecalhosDaMensagem) =>
            Iniciar(
                $"{destino} {OperacaoDeProcessamento}",
                ActivityKind.Consumer,
                LerContextoDeRastreamento(cabecalhosDaMensagem),
                destino,
                idDaMensagem,
                OperacaoDeProcessamento);

        private static Activity? Iniciar(
            string nomeDaAtividade,
            ActivityKind tipoDaAtividade,
            string? contextoDeRastreamentoDaOrigem,
            string destino,
            string? idDaMensagem,
            string operacao)
        {
            ActivityContext.TryParse(contextoDeRastreamentoDaOrigem, null, out var contextoDaOrigem);

            return FonteDeAtividades.StartActivity(
                nomeDaAtividade,
                tipoDaAtividade,
                contextoDaOrigem,
                [
                    new("messaging.system", SistemaDeMensageria),
                    new("messaging.destination.name", destino),
                    new("messaging.operation.type", operacao),
                    new("messaging.message.id", idDaMensagem)
                ]);
        }

        private static string? LerContextoDeRastreamento(IDictionary<string, object?>? cabecalhosDaMensagem)
        {
            if (cabecalhosDaMensagem is null || !cabecalhosDaMensagem.TryGetValue(CabecalhoDeContextoDeRastreamento, out var valor))
                return null;

            return valor switch
            {
                byte[] conteudo => Encoding.UTF8.GetString(conteudo),
                string texto => texto,
                _ => null
            };
        }
    }
}
