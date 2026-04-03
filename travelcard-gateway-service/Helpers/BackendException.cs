using System;

namespace TravelcardGatewayService.Helpers
{
    public class BackendException : Exception
    {
        public BackendException(string message) : base(message)
        {
        }
    }
}
