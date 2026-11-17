using System.Security.Cryptography;
using System.Text;

namespace Verity.Lancamentos.Core.Dominio.ObjetosDeValor
{
    public sealed class HashDaRequisicao
    {
        public const int TamanhoEmCaracteres = 64;

        private const char SeparadorDasPartesDaRequisicao = '|';

        private HashDaRequisicao(string valor)
        {
            Valor = valor;
        }

        public string Valor { get; }

        public static HashDaRequisicao Calcular(params string[] partesDaRequisicao)
        {
            var conteudoCanonicoDaRequisicao = string.Join(SeparadorDasPartesDaRequisicao, partesDaRequisicao);
            var bytesDoHash = SHA256.HashData(Encoding.UTF8.GetBytes(conteudoCanonicoDaRequisicao));
            return new HashDaRequisicao(Convert.ToHexStringLower(bytesDoHash));
        }

        public bool IgualA(string outroHash) => string.Equals(Valor, outroHash, StringComparison.Ordinal);
    }
}
