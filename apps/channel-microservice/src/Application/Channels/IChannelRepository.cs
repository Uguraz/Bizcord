using System.Threading;
using System.Threading.Tasks;
using ChannelMicroservice.Domain.Channels;

namespace ChannelMicroservice.Application.Channels
{
    public interface IChannelRepository
    {
        Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default);
        Task AddAsync(Channel channel, CancellationToken ct = default);
    }
}