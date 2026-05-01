using Microsoft.AspNetCore.Mvc;
using new_test_for_demo.Models;
using new_test_for_demo.Services;
using System.ComponentModel.DataAnnotations;

namespace new_test_for_demo.Controllers;

[ApiController]
[Route("travelcards")]
public class TravelcardsController : ControllerBase
{
    private readonly ITravelcardService _travelcardService;
    private readonly ILogger<TravelcardsController> _logger;

    public TravelcardsController(ITravelcardService travelcardService, ILogger<TravelcardsController> logger)
    {
        _travelcardService = travelcardService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromHeader(Name = "client_id")] string clientId, [FromBody] CreateTravelcardRequest request)
    {
        if (string.IsNullOrWhiteSpace(clientId) || clientId.Length > 128)
            return BadRequest(new { error = "client_id required, max 128 chars" });

        if (!RegexMatch(clientId, @"^[\w+]+$"))
            return BadRequest(new { error = "client_id invalid" });

        if (!Request.Headers.TryGetValue("Content-Type", out var contentType) || !contentType.ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "Content-Type must contain application/json" });

        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        try
        {
            var result = await _travelcardService.CreateAsync(request);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation failed");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error");
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    private static bool RegexMatch(string input, string pattern) => System.Text.RegularExpressions.Regex.IsMatch(input, pattern);
}