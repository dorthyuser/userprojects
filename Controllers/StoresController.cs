using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using hello_http_test.Services;
using hello_http_test.Models;

namespace hello_http_test.Controllers
{
    /// <summary>
    /// Controller to create, fetch and update store details in Zoho Europe Commerce.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class StoresController : ControllerBase
    {
        private readonly IZohoStoreService _service;
        private readonly ILogger<StoresController> _logger;

        /// <summary>
        /// Constructor with injected service and logger.
        /// </summary>
        public StoresController(IZohoStoreService service, ILogger<StoresController> logger)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Fetches stores from Zoho.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetStores()
        {
            try
            {
                var stores = await _service.GetStoresAsync();
                return Ok(stores);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching stores");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Creates a store in Zoho.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateStore([FromBody] Store store)
        {
            if (store == null)
                return BadRequest();

            try
            {
                var created = await _service.CreateStoreAsync(store);
                return CreatedAtAction(nameof(GetStores), new { }, created);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating store");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Updates a store in Zoho.
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStore(string id, [FromBody] Store store)
        {
            if (string.IsNullOrWhiteSpace(id) || store == null)
                return BadRequest();

            try
            {
                var updated = await _service.UpdateStoreAsync(id, store);
                return Ok(updated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating store with id {Id}", id);
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
