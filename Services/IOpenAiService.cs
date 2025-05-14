using System.Threading.Tasks;

namespace CodeVault.Services
{
    public interface IOpenAiService
    {
        Task<string> GetCompletionAsync(string prompt);
    }
}