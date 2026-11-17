using Verity.Compartilhado.Base;
using Verity.Lancamentos.Core.Dominio.Enumeradores;
using Verity.Lancamentos.Core.Dominio.Erros;
using Verity.Lancamentos.Core.Dominio.Eventos;
using Verity.Lancamentos.Core.Dominio.ObjetosDeValor;

namespace Verity.Lancamentos.Core.Dominio.Entidades
{
    public sealed class Lancamento : Entidade
    {
        private const string PrefixoDaDescricaoDeEstorno = "Estorno: ";

        private Lancamento(
            Guid id,
            string comercianteId,
            TipoDeLancamento tipo,
            decimal valor,
            DateOnly dataCompetencia,
            string descricao,
            Guid? estornoDeId,
            string chaveDeIdempotencia,
            string hashDaRequisicao,
            DateTime registradoEmUtc)
            : base(id)
        {
            ComercianteId = comercianteId;
            Tipo = tipo;
            Valor = valor;
            DataCompetencia = dataCompetencia;
            Descricao = descricao;
            EstornoDeId = estornoDeId;
            ChaveDeIdempotencia = chaveDeIdempotencia;
            HashDaRequisicao = hashDaRequisicao;
            RegistradoEmUtc = registradoEmUtc;
        }

        public string ComercianteId { get; private set; }

        public TipoDeLancamento Tipo { get; private set; }

        public decimal Valor { get; private set; }

        public DateOnly DataCompetencia { get; private set; }

        public string Descricao { get; private set; }

        public Guid? EstornoDeId { get; private set; }

        public string ChaveDeIdempotencia { get; private set; }

        public string HashDaRequisicao { get; private set; }

        public DateTime RegistradoEmUtc { get; private set; }

        public bool EhEstorno => EstornoDeId.HasValue;

        public static Lancamento Registrar(
            IdentificadorDoComerciante comerciante,
            TipoDeLancamento tipo,
            ValorDoLancamento valor,
            DataDeCompetencia dataCompetencia,
            DescricaoDoLancamento descricao,
            ChaveDeIdempotencia chaveDeIdempotencia,
            HashDaRequisicao hashDaRequisicao,
            DateTime instanteAtualUtc)
        {
            var lancamento = new Lancamento(
                Guid.CreateVersion7(),
                comerciante.Valor,
                tipo,
                valor.Valor,
                dataCompetencia.Valor,
                descricao.Valor,
                estornoDeId: null,
                chaveDeIdempotencia.Valor,
                hashDaRequisicao.Valor,
                instanteAtualUtc);

            lancamento.AdicionarEventoDeDominio(new LancamentoRegistrado(lancamento));
            return lancamento;
        }

        public Resultado<Lancamento> Estornar(
            ChaveDeIdempotencia chaveDeIdempotencia,
            HashDaRequisicao hashDaRequisicao,
            DateTime instanteAtualUtc)
        {
            if (EhEstorno)
                return Resultado<Lancamento>.Falha(CodigosDeErro.EstornoNaoPodeSerEstornado);

            var estorno = new Lancamento(
                Guid.CreateVersion7(),
                ComercianteId,
                Tipo.Inverso(),
                Valor,
                DataCompetencia,
                MontarDescricaoDoEstorno(),
                estornoDeId: Id,
                chaveDeIdempotencia.Valor,
                hashDaRequisicao.Valor,
                instanteAtualUtc);

            estorno.AdicionarEventoDeDominio(new LancamentoRegistrado(estorno));
            return Resultado<Lancamento>.Sucesso(estorno);
        }

        public bool FoiRegistradoComOMesmoConteudo(HashDaRequisicao hashDaRequisicao) =>
            hashDaRequisicao.IgualA(HashDaRequisicao);

        private string MontarDescricaoDoEstorno()
        {
            var descricaoDoEstorno = PrefixoDaDescricaoDeEstorno + Descricao;

            return descricaoDoEstorno.Length <= DescricaoDoLancamento.TamanhoMaximo
                ? descricaoDoEstorno
                : descricaoDoEstorno[..DescricaoDoLancamento.TamanhoMaximo];
        }
    }
}
