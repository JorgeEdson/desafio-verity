namespace Verity.Lancamentos.Core.UseCases.Compartilhado
{
    public sealed record RegistroDeLancamentoSaida(LancamentoSaida Lancamento, bool FoiCriadoNestaRequisicao);
}
