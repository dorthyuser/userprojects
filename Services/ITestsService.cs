using testing2.Models;

namespace testing2.Services;

public interface ITestsService
{
    Task<TestResponse> CreateAsync(TestRequest request, CancellationToken cancellationToken = default);
}