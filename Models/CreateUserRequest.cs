using System;

namespace ZohoProject3.Models
{
    public class CreateUserRequest
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Role { get; set; }
    }
}
