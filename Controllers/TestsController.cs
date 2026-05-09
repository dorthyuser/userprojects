using Microsoft.AspNetCore.Mvc;
using testing2.Models;
using testing2.Services;

namespace testing2.Controllers;

[ApiController]
[Route("tests")]
public sealed class TestsController : ControllerBase
{
    private readonly ITestsService _service;

    public TestsController(ITestsService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<ActionResult<TestResponse>> CreateAsync([FromBody] TestRequest request, CancellationToken cancellationToken)
    {
        var response = await _service.CreateAsync(request, cancellationToken);
        return Ok(response);
    }
}