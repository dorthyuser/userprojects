using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using ZohoOauthKvTest.Services;

namespace ZohoOauthKvTest.Controllers
{
    [ApiController]
    public class LeadsController : ControllerBase
    {
        private readonly IZohoHttpService _zohoHttpService;

        public LeadsController(IZohoHttpService zohoHttpService)
        {
            _zohoHttpService = zohoHttpService;
        }

        [HttpGet]
        [Route("crm/v2/Leads")]
        public async Task<IActionResult> GetLeads()
        {
            try
            {
                var result = await _zohoHttpService.GetLeadsAsync();
                return new ContentResult
                {
                    StatusCode = (int)result.StatusCode,
                    Content = await result.Content.ReadAsStringAsync(),
                    ContentType = result.Content.Headers.ContentType?.ToString() ?? "application/json"
                };
            }
            catch (System.Exception ex)
            {
                return Problem(detail: ex.Message, statusCode: 500);
            }
        }
    }
}
