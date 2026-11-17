namespace Verity.Lancamentos.Core.Contratos
{
    public interface IRelogioDoComerciante
    {
        DateOnly ObterDataDeHoje();

        DateTime ObterInstanteAtualUtc();
    }
}
