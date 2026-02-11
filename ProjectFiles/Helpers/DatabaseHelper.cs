using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AccountsFunction.Models;

namespace AccountsFunction.Helpers
{
 // Simple in-memory database helper for development and tests.
 public class DatabaseHelper
 {
 private readonly ConcurrentDictionary<Guid, Account> _store = new ConcurrentDictionary<Guid, Account>();

 public DatabaseHelper()
 {
 // Seed with a sample account for convenience
 var sample = new Account
 {
 Id = Guid.NewGuid(),
 Name = "Sample Account",
 Email = "sample@example.com",
 Address = "123 Sample St"
 };
 _store[sample.Id] = sample;
 }

 // Static helper for parsing GUIDs used by the function
 public static bool TryParseGuid(string input, out Guid guid)
 {
 return Guid.TryParse(input, out guid);
 }

 public Task<Account?> GetAccountByIdAsync(Guid id)
 {
 _store.TryGetValue(id, out var acc);
 // Return a copy to avoid accidental modification by caller
 if (acc == null) return Task.FromResult<Account?>(null);
 var copy = new Account
 {
 Id = acc.Id,
 Name = acc.Name,
 Email = acc.Email,
 Address = acc.Address
 };
 return Task.FromResult<Account?>(copy);
 }

 public Task<List<Account>> CreateAccountsAsync(IEnumerable<Account> accounts)
 {
 var created = new List<Account>();
 foreach (var acc in accounts)
 {
 if (acc == null) continue;

 var toStore = new Account
 {
 Id = acc.Id,
 Name = acc.Name,
 Email = acc.Email,
 Address = acc.Address
 };

 // Upsert semantics: add or update
 _store.AddOrUpdate(toStore.Id, toStore, (k, v) => toStore);
 created.Add(toStore);
 }

 return Task.FromResult(created);
 }
 }
}
