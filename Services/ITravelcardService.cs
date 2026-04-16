using System.Threading.Tasks;

namespace TcTestingZoho.Services
{
    public interface ITravelcardService
    {
        Task<ForwardResult> ForwardAsync(string body);
    }

    public class ForwardResult
    {
        public bool IsSuccess { get; set; }
        public int StatusCode { get; set; }
        public string Content { get; set; }
        public string Error { get; set; }
    }
}
