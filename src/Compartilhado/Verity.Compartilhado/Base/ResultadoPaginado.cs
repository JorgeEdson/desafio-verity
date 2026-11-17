namespace Verity.Compartilhado.Base
{
    public sealed record ResultadoPaginado<T>(
        IReadOnlyCollection<T> Itens,
        int Pagina,
        int TamanhoDaPagina,
        int TotalDeItens)
    {
        public int TotalDePaginas => TotalDeItens == 0
            ? 0
            : (int)Math.Ceiling(TotalDeItens / (double)TamanhoDaPagina);
    }
}
