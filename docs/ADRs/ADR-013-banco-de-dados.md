# ADR-013 — PostgreSQL com um banco por serviço

- **Status:** Aceito
- **Data:** 2026-11-16

## Contexto

Os dois contextos precisam de persistência transacional. O isolamento de falha do [ADR-001](ADR-001-estilo-arquitetural.md) perde sentido se os serviços compartilharem esquema ou se um puder ler as tabelas do outro. O consolidado depende de recursos específicos: `INSERT ... ON CONFLICT`, locks consultivos, `FOR UPDATE SKIP LOCKED` (no outbox) e tipos de data e decimais precisos.

## Decisão

- **PostgreSQL** para os dois serviços.
- **Um banco por serviço** (`lancamentos_db`, `consolidado_db`), com usuários próprios e permissões mínimas ([Segurança §4.4](../04-seguranca.md)). Um serviço **nunca** acessa o banco do outro; a única integração é o evento.
- Localmente, por economia, **um** container Postgres hospeda os dois databases. Em produção, **servidores separados**, para que a carga ou a queda de um não afete o outro.
- Valores monetários em `numeric(18,2)` (nunca `float`); datas de competência em `date`; instantes em `timestamptz` (UTC).
- EF Core + Npgsql, com migrations versionadas. O SQL da consolidação (upsert e propagação) é escrito à mão e parametrizado, por clareza e controle.

## Alternativas consideradas

| Alternativa | Por que não |
|---|---|
| **SQL Server** | Ótimo no ecossistema .NET, mas a imagem é mais pesada no compose, e os recursos usados (`SKIP LOCKED`, advisory locks, `ON CONFLICT`) são mais diretos no Postgres. A troca seria viável. |
| **Banco único compartilhado** | Acoplamento por esquema; a carga de leitura cairia sobre o banco transacional; a queda do banco derrubaria os dois serviços. |
| **NoSQL para o consolidado** (Redis, Mongo) | O modelo é tabular, e a propagação retroativa precisa de atualização transacional de várias linhas junto com a idempotência. |

## Consequências

**Positivas**
- Isolamento real de dados e de falhas em produção.
- Um único motor de banco para operar, aprender e monitorar.

**Negativas**
- Não há consultas que cruzem os dois contextos: relatórios que precisem dos dois dados exigem compor via API ou criar um novo modelo de leitura.
- Localmente, o container único é um ponto comum de falha, aceito apenas em desenvolvimento e documentado em [Implantação](../05-implantacao.md).
