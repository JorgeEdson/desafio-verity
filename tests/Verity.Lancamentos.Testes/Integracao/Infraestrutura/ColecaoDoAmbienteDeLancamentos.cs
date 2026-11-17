namespace Verity.Lancamentos.Testes.Integracao.Infraestrutura
{
    [CollectionDefinition(Nome)]
    public sealed class ColecaoDoAmbienteDeLancamentos : ICollectionFixture<AmbienteDeLancamentosFixture>
    {
        public const string Nome = "Ambiente de lançamentos com PostgreSQL";
    }
}
