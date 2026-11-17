using Microsoft.EntityFrameworkCore;
using Verity.Consolidado.Core.Contratos;
using Verity.Consolidado.Core.Dominio.Enumeradores;
using Verity.Consolidado.Core.Dominio.ObjetosDeValor;

namespace Verity.Consolidado.Infraestrutura.Persistencia
{
    public sealed class ConsolidacaoDeMovimentos(ConsolidadoDbContext contexto, TimeProvider provedorDeTempo)
        : IConsolidacaoDeMovimentos
    {
        private const int QuantidadeDeLinhasInseridasParaEventoInedito = 1;

        public async Task<ResultadoDaConsolidacao> ConsolidarAsync(MovimentoDoLancamento movimento, CancellationToken cancellationToken)
        {
            var instanteAtualUtc = provedorDeTempo.GetUtcNow().UtcDateTime;

            await using var transacao = await contexto.Database.BeginTransactionAsync(cancellationToken);

            await SerializarMovimentosDoComercianteAsync(movimento.ComercianteId, cancellationToken);

            var eventoInedito = await RegistrarEventoComoProcessadoAsync(movimento.EventoId, instanteAtualUtc, cancellationToken);

            if (!eventoInedito)
            {
                await transacao.RollbackAsync(cancellationToken);
                return ResultadoDaConsolidacao.EventoJaConsolidadoAnteriormente;
            }

            await GarantirSaldoDoDiaDeCompetenciaAsync(movimento, instanteAtualUtc, cancellationToken);
            await AplicarMovimentoNoDiaDeCompetenciaAsync(movimento, instanteAtualUtc, cancellationToken);
            await PropagarVariacaoParaOsDiasSeguintesAsync(movimento, instanteAtualUtc, cancellationToken);

            await transacao.CommitAsync(cancellationToken);
            return ResultadoDaConsolidacao.MovimentoConsolidado;
        }

        private Task SerializarMovimentosDoComercianteAsync(string comercianteId, CancellationToken cancellationToken) =>
            contexto.Database.ExecuteSqlAsync(
                $"SELECT pg_advisory_xact_lock(hashtext({comercianteId}))",
                cancellationToken);

        private async Task<bool> RegistrarEventoComoProcessadoAsync(Guid eventoId, DateTime instanteAtualUtc, CancellationToken cancellationToken)
        {
            var quantidadeDeLinhasInseridas = await contexto.Database.ExecuteSqlAsync(
                $"""
                INSERT INTO evento_processado (evento_id, processado_em_utc)
                VALUES ({eventoId}, {instanteAtualUtc})
                ON CONFLICT (evento_id) DO NOTHING
                """,
                cancellationToken);

            return quantidadeDeLinhasInseridas == QuantidadeDeLinhasInseridasParaEventoInedito;
        }

        private Task GarantirSaldoDoDiaDeCompetenciaAsync(MovimentoDoLancamento movimento, DateTime instanteAtualUtc, CancellationToken cancellationToken) =>
            contexto.Database.ExecuteSqlAsync(
                $"""
                INSERT INTO saldo_diario (comerciante_id, data, total_creditos, total_debitos, saldo_final, atualizado_em_utc)
                VALUES (
                    {movimento.ComercianteId},
                    {movimento.DataCompetencia},
                    0,
                    0,
                    COALESCE((
                        SELECT saldo_anterior.saldo_final
                        FROM saldo_diario AS saldo_anterior
                        WHERE saldo_anterior.comerciante_id = {movimento.ComercianteId}
                          AND saldo_anterior.data < {movimento.DataCompetencia}
                        ORDER BY saldo_anterior.data DESC
                        LIMIT 1), 0),
                    {instanteAtualUtc})
                ON CONFLICT (comerciante_id, data) DO NOTHING
                """,
                cancellationToken);

        private Task AplicarMovimentoNoDiaDeCompetenciaAsync(MovimentoDoLancamento movimento, DateTime instanteAtualUtc, CancellationToken cancellationToken) =>
            contexto.Database.ExecuteSqlAsync(
                $"""
                UPDATE saldo_diario
                SET total_creditos = total_creditos + {movimento.TotalDeCreditos},
                    total_debitos = total_debitos + {movimento.TotalDeDebitos},
                    saldo_final = saldo_final + {movimento.VariacaoDoSaldo},
                    atualizado_em_utc = {instanteAtualUtc}
                WHERE comerciante_id = {movimento.ComercianteId}
                  AND data = {movimento.DataCompetencia}
                """,
                cancellationToken);

        private Task PropagarVariacaoParaOsDiasSeguintesAsync(MovimentoDoLancamento movimento, DateTime instanteAtualUtc, CancellationToken cancellationToken) =>
            contexto.Database.ExecuteSqlAsync(
                $"""
                UPDATE saldo_diario
                SET saldo_final = saldo_final + {movimento.VariacaoDoSaldo},
                    atualizado_em_utc = {instanteAtualUtc}
                WHERE comerciante_id = {movimento.ComercianteId}
                  AND data > {movimento.DataCompetencia}
                """,
                cancellationToken);
    }
}
