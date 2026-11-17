using Verity.Lancamentos.Core.Dominio.Entidades;
using Verity.Lancamentos.Core.Dominio.Enumeradores;
using Verity.Lancamentos.Core.Dominio.ObjetosDeValor;

namespace Verity.Lancamentos.Testes.Compartilhado
{
    public sealed class LancamentoBuilder
    {
        private static readonly DateOnly DataDeCompetenciaPadrao = new(2026, 11, 10);
        private static readonly DateTime InstanteDeRegistroPadraoUtc = new(2026, 11, 10, 15, 0, 0, DateTimeKind.Utc);

        private string _comerciante = "c-teste";
        private TipoDeLancamento _tipo = TipoDeLancamento.Credito;
        private decimal _valor = 150.00m;
        private DateOnly _dataCompetencia = DataDeCompetenciaPadrao;
        private string _descricao = "Venda no balcão";
        private string _chaveDeIdempotencia = Guid.NewGuid().ToString();

        public LancamentoBuilder DoComerciante(string comerciante)
        {
            _comerciante = comerciante;
            return this;
        }

        public LancamentoBuilder DoTipo(TipoDeLancamento tipo)
        {
            _tipo = tipo;
            return this;
        }

        public LancamentoBuilder ComValor(decimal valor)
        {
            _valor = valor;
            return this;
        }

        public LancamentoBuilder ComDataDeCompetencia(DateOnly dataCompetencia)
        {
            _dataCompetencia = dataCompetencia;
            return this;
        }

        public LancamentoBuilder ComDescricao(string descricao)
        {
            _descricao = descricao;
            return this;
        }

        public Lancamento Construir() => Lancamento.Registrar(
            IdentificadorDoComerciante.TentarCriar(_comerciante).Instancia,
            _tipo,
            ValorDoLancamento.TentarCriar(_valor).Instancia,
            DataDeCompetencia.TentarCriar(_dataCompetencia, _dataCompetencia).Instancia,
            DescricaoDoLancamento.TentarCriar(_descricao).Instancia,
            ChaveDeIdempotencia.TentarCriar(_chaveDeIdempotencia).Instancia,
            HashDaRequisicao.Calcular(_chaveDeIdempotencia),
            InstanteDeRegistroPadraoUtc);
    }
}
