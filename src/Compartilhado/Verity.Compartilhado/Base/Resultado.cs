namespace Verity.Compartilhado.Base
{
    public class Resultado
    {
        protected Resultado(IReadOnlyCollection<string> erros)
        {
            Erros = erros;
        }

        public IReadOnlyCollection<string> Erros { get; }

        public bool EhSucesso => Erros.Count == 0;

        public bool EhFalha => !EhSucesso;

        public static Resultado Sucesso() => new([]);

        public static Resultado Falha(params string[] erros) => new(erros);

        public static Resultado Falha(IEnumerable<string> erros) => new(erros.ToArray());

        public static Resultado Combinar(params Resultado[] resultados) =>
            new(resultados.SelectMany(resultado => resultado.Erros).ToArray());

        public bool PossuiErro(string codigoDeErro) => Erros.Contains(codigoDeErro);
    }

    public sealed class Resultado<T> : Resultado
    {
        private readonly T? _instancia;

        private Resultado(T? instancia, IReadOnlyCollection<string> erros) : base(erros)
        {
            _instancia = instancia;
        }

        public T Instancia => EhSucesso
            ? _instancia!
            : throw new InvalidOperationException("Não é possível acessar a instância de um resultado com falha.");

        public static Resultado<T> Sucesso(T instancia) => new(instancia, []);

        public static new Resultado<T> Falha(params string[] erros) => new(default, erros);

        public static new Resultado<T> Falha(IEnumerable<string> erros) => new(default, erros.ToArray());
    }
}
