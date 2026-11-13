using Pr02Ber.Models;

namespace Pr02Ber.Services
{
    public interface IProductRepository
    {
        void Add(Product product);
        IReadOnlyList<Product> GetAll();
        Product? GetBySku(string sku);
        int Count { get; }
        void Clear();
    }
}