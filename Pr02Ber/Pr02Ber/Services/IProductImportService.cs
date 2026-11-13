using Pr02Ber.Models;

namespace Pr02Ber.Services
{
    public interface IProductImportService
    {
        ImportResult ImportFromCsv(string filePath);
        ImportResult ImportFromLines(IEnumerable<string> lines);
    }
}