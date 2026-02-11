using Xunit;
using Microsoft.AspNetCore.Mvc;
using Project.SalesforceController.Controllers;
using Project.SalesforceController.Services;
using Project.SalesforceController.Models;

namespace Project.SalesforceController.Tests.Controllers.Salesforce;

public class SalesforceControllerTests
{
 private readonly ISalesforceService _service;
 private readonly SalesforceController _controller;

 public SalesforceControllerTests()
 {
 _service = new InMemorySalesforceService();
 _controller = new SalesforceController(_service);
 }

 [Fact]
 public void GetAccounts_DefaultPagination_ReturnsJsonResultWithPagedData()
 {
 // default page=1, pageSize=10
 var result = _controller.GetAccounts();
 var json = Assert.IsType<JsonResult>(result);
 var paged = Assert.IsType<PagedResult<AccountDto>>(json.Value);
 Assert.Equal(1, paged.Page);
 Assert.Equal(10, paged.PageSize);
 Assert.Equal(50, paged.TotalCount);
 Assert.Equal(10, paged.Items.Count());
 }

 [Fact]
 public void GetAccounts_CustomPagination_ReturnsCorrectPage()
 {
 var result = _controller.GetAccounts(page: 2, pageSize: 15);
 var json = Assert.IsType<JsonResult>(result);
 var paged = Assert.IsType<PagedResult<AccountDto>>(json.Value);
 Assert.Equal(2, paged.Page);
 Assert.Equal(15, paged.PageSize);
 Assert.Equal(50, paged.TotalCount);
 Assert.Equal(15, paged.Items.Count());
 // first item on page 2 should be item index 15 (1-based -> id "16")
 Assert.Equal("16", paged.Items.First().Id);
 }

 [Fact]
 public void GetAccountById_Existing_ReturnsJsonResult()
 {
 var result = _controller.GetAccountById("1");
 var json = Assert.IsType<JsonResult>(result);
 var account = Assert.IsType<AccountDto>(json.Value);
 Assert.Equal("1", account.Id);
 }

 [Fact]
 public void CreateUpdateDelete_Workflow_WorksAsExpected()
 {
 var newAccount = new AccountDto { Id = "900", Name = "New Account", Phone = "123", Website = "ex.com" };
 var createResult = _controller.CreateAccount(newAccount);
 var createJson = Assert.IsType<JsonResult>(createResult);
 var created = Assert.IsType<AccountDto>(createJson.Value);
 Assert.Equal("900", created.Id);

 // Update
 var updateDto = new AccountDto { Name = "Updated", Phone = "999" };
 var updateResult = _controller.UpdateAccount("900", updateDto);
 var updateJson = Assert.IsType<JsonResult>(updateResult);
 var updated = Assert.IsType<AccountDto>(updateJson.Value);
 Assert.Equal("Updated", updated.Name);

 // Delete
 var deleteResult = _controller.DeleteAccount("900");
 var deleteJson = Assert.IsType<JsonResult>(deleteResult);
 var deleteValue = Assert.IsType<System.Collections.Generic.Dictionary<string, object>>(deleteJson.Value);
 Assert.True(deleteValue.ContainsKey("success"));
 }
}
