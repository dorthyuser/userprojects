using System.Linq;
using Project.SalesforceApi.Controllers;
using Project.SalesforceApi.Models;
using Project.SalesforceApi.Services;
using Xunit;

namespace Project.SalesforceController.Tests.Controllers.Salesforce
{
 public class SalesforceControllerTests
 {
 private class FakeSalesforceService : ISalesforceService
 {
 private readonly List<AccountDto> _data;
 public FakeSalesforceService()
 {
 _data = new List<AccountDto>();
 for (int i = 1; i <= 25; i++)
 {
 _data.Add(new AccountDto { Id = i.ToString(), Name = $"A{i}", Phone = $"P{i}", Industry = "X" });
 }
 }

 public PagedResult<AccountDto> GetAccounts(int page, int pageSize)
 {
 var total = _data.Count;
 var items = _data.Skip((page - 1) * pageSize).Take(pageSize).ToList();
 return new PagedResult<AccountDto>
 {
 Page = page,
 PageSize = pageSize,
 TotalCount = total,
 Items = items
 };
 }

 public AccountDto? GetById(string id) => _data.FirstOrDefault(a => a.Id == id);

 public AccountDto Create(AccountDto dto)
 {
 dto.Id = (_data.Count + 1).ToString();
 _data.Add(dto);
 return dto;
 }

 public bool Update(string id, AccountDto dto)
 {
 var e = _data.FirstOrDefault(a => a.Id == id);
 if (e == null) return false;
 e.Name = dto.Name;
 e.Phone = dto.Phone;
 e.Industry = dto.Industry;
 return true;
 }

 public bool Delete(string id)
 {
 var e = _data.FirstOrDefault(a => a.Id == id);
 if (e == null) return false;
 _data.Remove(e);
 return true;
 }
 }

 [Fact]
 public void Get_WithPagination_Returns_CorrectPage()
 {
 // Arrange
 var service = new FakeSalesforceService();
 var controller = new SalesforceController(service);

 // Act
 var result = controller.Get(page: 2, pageSize: 10);

 // Assert
 var ok = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(result.Result);
 var paged = Assert.IsType<PagedResult<AccountDto>>(ok.Value);
 Assert.Equal(2, paged.Page);
 Assert.Equal(10, paged.PageSize);
 Assert.Equal(25, paged.TotalCount);
 Assert.Equal(10, paged.Items.Count());
 Assert.Equal("11", paged.Items.First().Id);
 }

 [Fact]
 public void Create_Update_Delete_Works()
 {
 var service = new FakeSalesforceService();
 var controller = new SalesforceController(service);

 var toCreate = new AccountDto { Name = "New", Phone = "P", Industry = "I" };
 var createResult = controller.Create(toCreate);
 var created = Assert.IsType<Microsoft.AspNetCore.Mvc.CreatedAtActionResult>(createResult.Result);
 var createdAccount = Assert.IsType<AccountDto>(created.Value);
 Assert.NotNull(createdAccount.Id);

 // Update
 var updateDto = new AccountDto { Name = "Updated", Phone = "P2", Industry = "I2" };
 var updateResponse = controller.Update(createdAccount.Id!, updateDto);
 Assert.IsType<Microsoft.AspNetCore.Mvc.NoContentResult>(updateResponse);

 // GetById
 var getById = controller.GetById(createdAccount.Id!);
 var ok = Assert.IsType<Microsoft.AspNetCore.Mvc.OkObjectResult>(getById.Result);
 var fetched = Assert.IsType<AccountDto>(ok.Value);
 Assert.Equal("Updated", fetched.Name);

 // Delete
 var del = controller.Delete(createdAccount.Id!);
 Assert.IsType<Microsoft.AspNetCore.Mvc.NoContentResult>(del);

 // Ensure not found after delete
 var getAfterDelete = controller.GetById(createdAccount.Id!);
 Assert.IsType<Microsoft.AspNetCore.Mvc.NotFoundResult>(getAfterDelete.Result);
 }
 }
}
