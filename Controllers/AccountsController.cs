using Microsoft.AspNetCore.Mvc;
using test_sf_git_prop.Models;
using test_sf_git_prop.Services;

namespace test_sf_git_prop.Controllers;

[ApiController]
[Route("api")]
public class AccountsController : ControllerBase
{
    private readonly IAccountService _accountService;
    private readonly ILogger<AccountsController> _logger;

    public AccountsController(IAccountService accountService, ILogger<AccountsController> logger)
    {
        _accountService = accountService;
        _logger = logger;
    }

    [HttpGet("accounts")]
    [ProducesResponseType(typeof(IEnumerable<AccountDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAccounts(CancellationToken cancellationToken)
    {
        try
        {
            var accounts = await _accountService.GetAccountsAsync(cancellationToken);
            return Ok(accounts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting accounts from Salesforce.");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while retrieving accounts." });
        }
    }

    [HttpPost("accounts")]
    [ProducesResponseType(typeof(AccountDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateAccount([FromBody] CreateAccountRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var created = await _accountService.CreateAccountAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetAccounts), new { id = created.Id }, created);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error while creating account.");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating account in Salesforce.");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while creating the account." });
        }
    }

    [HttpPut("accounts/{id}")]
    [ProducesResponseType(typeof(AccountDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateAccount([FromRoute] string id, [FromBody] UpdateAccountRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var updated = await _accountService.UpdateAccountAsync(id, request, cancellationToken);
            return Ok(updated);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Account not found for update: {Id}", id);
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error while updating account: {Id}", id);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating account in Salesforce: {Id}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while updating the account." });
        }
    }
}