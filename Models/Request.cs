using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace UpTransportTicketApiLambda.Models
{
    public class Request
    {
        public Request()
        {
            Name = string.Empty;
            Categories = new List<Category>();
        }

        // Optional for create, required for update/delete via path id
        public Guid? Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public List<Category> Categories { get; set; }
    }
}
