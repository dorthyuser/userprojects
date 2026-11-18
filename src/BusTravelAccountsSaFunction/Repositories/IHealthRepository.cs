using System.Threading;
using System.Threading.Tasks;

namespace BusTravelAccountsSaFunction;

public interface IHealthRepository
{
    Task CheckDependenciesAsync(CancellationToken cancellationToken);
}
