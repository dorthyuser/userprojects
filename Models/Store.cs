using System;

namespace hello_http_test.Models
{
    /// <summary>
    /// Represents a store in Zoho Commerce.
    /// Keep properties minimal and extensible to map Zoho store payloads.
    /// </summary>
    public class Store
    {
        /// <summary>
        /// Store identifier.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Store name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Store domain or subdomain.
        /// </summary>
        public string Domain { get; set; }

        /// <summary>
        /// Additional metadata container.
        /// </summary>
        public object Metadata { get; set; }
    }
}
