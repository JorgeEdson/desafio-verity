using System.Text.Json;

namespace Verity.Compartilhado.Integracao
{
    public static class SerializacaoDeEventosDeIntegracao
    {
        public static JsonSerializerOptions Opcoes { get; } = new(JsonSerializerDefaults.Web);

        public static string Serializar<TEventoDeIntegracao>(TEventoDeIntegracao eventoDeIntegracao) =>
            JsonSerializer.Serialize(eventoDeIntegracao, Opcoes);

        public static TEventoDeIntegracao? Desserializar<TEventoDeIntegracao>(string conteudo) =>
            JsonSerializer.Deserialize<TEventoDeIntegracao>(conteudo, Opcoes);
    }
}
