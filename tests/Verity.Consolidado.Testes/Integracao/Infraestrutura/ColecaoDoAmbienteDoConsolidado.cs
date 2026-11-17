namespace Verity.Consolidado.Testes.Integracao.Infraestrutura
{
    [CollectionDefinition(Nome)]
    public sealed class ColecaoDoAmbienteDoConsolidado : ICollectionFixture<AmbienteDoConsolidadoFixture>
    {
        public const string Nome = "Ambiente do consolidado com PostgreSQL";
    }
}
