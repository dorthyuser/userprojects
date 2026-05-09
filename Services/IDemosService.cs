using new_project.Models;

namespace new_project.Services;

public interface IDemosService
{
    Task<IReadOnlyList<DemoResponse>> GetAsync(CancellationToken cancellationToken = default);
}