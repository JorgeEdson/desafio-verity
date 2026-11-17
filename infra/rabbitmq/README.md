# RabbitMQ — topologia versionada

A topologia de mensageria é **infraestrutura como código**: [`definitions.json`](definitions.json) é carregado na inicialização do broker (via [`verity.conf`](verity.conf)). Os serviços **não declaram** exchanges nem filas; eles só publicam ou consomem. Isso permite dar a cada serviço apenas a permissão mínima de que ele precisa ([Segurança §4.4](../../docs/04-seguranca.md)).

## Topologia

| Recurso | Tipo | Configuração |
|---|---|---|
| `verity.lancamentos` | exchange `topic` | Recebe `LancamentoRegistrado` com a chave `lancamento.registrado` |
| `consolidado.lancamentos` | quorum queue | Ligada à exchange acima; `x-delivery-limit = 5`; mensagens rejeitadas seguem para a DLX |
| `verity.lancamentos.dlx` | exchange `fanout` | Dead-letter exchange |
| `consolidado.lancamentos.dlq` | quorum queue | Mensagens que excederam o limite de entregas, para análise |

A publicação usa `mandatory: true` com confirmação do broker. Se a exchange não existir ou não houver fila ligada a ela, a publicação falha e o evento **permanece pendente no outbox**. Nenhum evento é descartado silenciosamente.

## Usuários (somente desenvolvimento)

| Usuário | Senha | Permissões |
|---|---|---|
| `admin` | `admin` | Administrador (console em http://localhost:15672) |
| `lancamentos` | `lancamentos` | Somente **escrita** na exchange `verity.lancamentos` |
| `consolidado` | `consolidado` | Somente **leitura** da fila `consolidado.lancamentos` |

As senhas estão no arquivo como hash SHA-256 com salt (formato `rabbit_password_hashing_sha256`). Em produção, os usuários e as senhas vêm de um cofre de segredos, nunca do repositório.

## Alterando a topologia

As definições são aplicadas na inicialização do broker. Depois de editar o arquivo:

```bash
docker compose restart rabbitmq
```
