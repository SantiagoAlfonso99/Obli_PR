
using RabbitMQ.Client;

namespace Server.Handlers
{
    public interface IHandler
    {
        public Task Call(IModel channel, CancellationToken token);
    }
}