using RabbitMQ.Client;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;

namespace Verity.Testes.Compartilhado.Conteineres
{
    public static class ConteineresDeTeste
    {
        private const string ImagemDoPostgreSql = "postgres:17";
        private const string ImagemDoRabbitMq = "rabbitmq:4.1-management";

        public static PostgreSqlContainer CriarPostgreSql() =>
            new PostgreSqlBuilder().WithImage(ImagemDoPostgreSql).Build();

        public static RabbitMqContainer CriarRabbitMq() =>
            new RabbitMqBuilder().WithImage(ImagemDoRabbitMq).Build();

        public static Task<IConnection> CriarConexaoAsync(this RabbitMqContainer rabbitMq) =>
            new ConnectionFactory { Uri = new Uri(rabbitMq.GetConnectionString()) }.CreateConnectionAsync();
    }
}
