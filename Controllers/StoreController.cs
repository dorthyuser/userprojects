using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using hello_http_test.Models;
using hello_http_test.Services;

namespace hello_http_test.Controllers
{
    /// <summary>
    /// Controller exposing endpoints to create, fetch and update store details in Zoho Commerce.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class StoreController : ControllerBase
    {
        private readonly IStoreService _storeService;
        private readonly ILogger<StoreController> _logger;

        /// <summary>
        /// Constructor.
        /// </summary>
        public StoreController(IStoreService storeService, ILogger<StoreController> logger)
        {
            _storeService = storeService ?? throw new ArgumentNullException(nameof(storeService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Fetch the list of stores from Zoho.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetStores()
        {
            var result = await _storeService.GetStoresAsync();
            return Ok(result);
        }

        /// <summary>
        /// Create a new store in Zoho.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateStore([FromBody] StoreDto store)
        {
            if (store == null) return BadRequest("Store payload required.");
            var created = await _storeService.CreateStoreAsync(store);
            return CreatedAtAction(nameof(GetStores), created);
        }

        /// <summary>
        /// Update an existing store in Zoho.
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStore(string id, [FromBody] StoreDto store)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest("Id required.");
            if (store == null) return BadRequest("Store payload required.");
            var updated = await _storeService.UpdateStoreAsync(id, store);
            return Ok(updated);
        }
    }
}
