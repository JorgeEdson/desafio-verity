using System.ComponentModel.DataAnnotations;

namespace Verity.Lancamentos.Api.Modelos
{
    public sealed record RegistrarLancamentoRequest(
        [Required] string Tipo,
        [Required] decimal? Valor,
        [Required] DateOnly? DataCompetencia,
        [Required] string Descricao);
}
