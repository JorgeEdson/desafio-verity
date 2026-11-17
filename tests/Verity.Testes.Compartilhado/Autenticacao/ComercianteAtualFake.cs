using Verity.Compartilhado.Aplicacao;

namespace Verity.Testes.Compartilhado.Autenticacao
{
    public sealed class ComercianteAtualFake(string identificador) : IComercianteAtual
    {
        public string Identificador { get; } = identificador;
    }
}
