using Microsoft.AspNetCore.Mvc;
using Project.SalesforceController.Models;
using Project.SalesforceController.Services;

namespace Project.SalesforceController.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SalesforceController : ControllerBase
{
 private readonly ISalesforceService _service;

 public SalesforceController(ISalesforceService service)
 {
 _service = service;
 }

 // GET api/salesforce?page=1&pageSize=10
 [HttpGet]
 public IActionResult GetAccounts([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
 {
 if (page < 1) page = 1;
 if (pageSize < 1) pageSize = 10;

 var result = _service.GetAccounts(page, pageSize);
 return new JsonResult(result);
 }

 // GET api/salesforce/{id}
 [HttpGet("{id}")]
 public IActionResult GetAccountById(string id)
 {
 var account = _service.GetById(id);
 if (account == null) return NotFound();
 return new JsonResult(account);
 }

 // POST api/salesforce
 [HttpPost]
 public IActionResult CreateAccount([FromBody] AccountDto dto)
 {
 var created = _service.Create(dto);
 return new JsonResult(created);
 }

 // PUT api/salesforce/{id}
 [HttpPut("{id}")]
 public IActionResult UpdateAccount(string id, [FromBody] AccountDto dto)
 {
 var updated = _service.Update(id, dto);
 if (updated == null) return NotFound();
 return new JsonResult(updated);
 }

 // DELETE api/salesforce/{id}
 [HttpDelete("{id}")]
 public IActionResult DeleteAccount(string id)
 {
 var removed = _service.Delete(id);
 if (!removed) return NotFound();
 return new JsonResult(new { success = true });
 }
}
