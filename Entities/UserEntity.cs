using System;

namespace AgeApi.Entities
{
    public class UserEntity
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public DateTime DateOfBirth { get; set; }
        public DateTime RequestedAt { get; set; }
    }
}
