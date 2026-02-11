using Microsoft.AspNetCore.Mvc;
using Project.SalesforceApi.Models;
using Project.SalesforceApi.Services;

namespace Project.SalesforceApi.Controllers
{
 [ApiController]
 [Route("api/[controller]")]
 public class SalesforceController : ControllerBase
 {
 private readonly ISalesforceService _service;

 public SalesforceController(ISalesforceService service)
 {
 _service = service;
 }

 // GET: api/Salesforce?page=1&pageSize=10
 [HttpGet]
 public ActionResult<PagedResult<AccountDto>> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
 {
 if (page < 1) page = 1;
 if (pageSize < 1) pageSize = 10;

 var result = _service.GetAccounts(page, pageSize);
 return Ok(result);
 }

 // GET: api/Salesforce/{id}
 [HttpGet("{id}")]
 public ActionResult<AccountDto> GetById(string id)
 {
 var account = _service.GetById(id);
 if (account == null) return NotFound();
 return Ok(account);
 }

 // POST: api/Salesforce
 [HttpPost]
 public ActionResult<AccountDto> Create([FromBody] AccountDto dto)
 {
 var created = _service.Create(dto);
 return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
 }

 // PUT: api/Salesforce/{id}
 [HttpPut("{id}")]
 public IActionResult Update(string id, [FromBody] AccountDto dto)
 {
 var ok = _service.Update(id, dto);
 if (!ok) return NotFound();
 return NoContent();
 }

 // DELETE: api/Salesforce/{id}
 [HttpDelete("{id}")]
 public IActionResult Delete(string id)
 {
 var ok = _service.Delete(id);
 if (!ok) return NotFound();
 return NoContent();
 }
 }
}
