using System.Threading;
using System.Threading.Tasks;

namespace CommandCenter.Domain.Interfaces;

public interface IPubSubService
{
    Task PublishAsync<T>(string topicId, T message, CancellationToken ct = default) where T : class;
}
