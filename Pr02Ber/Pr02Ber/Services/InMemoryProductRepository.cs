using Pr02Ber.Models;

namespace Pr02Ber.Services
{
    public class InMemoryProductRepository : IProductRepository
    {
        private readonly List<Product> _products = new();
        private readonly object _lock = new();

        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _products.Count;
                }
            }
        }

        public void Add(Product product)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            lock (_lock)
            {
                _products.Add(product);
            }
        }

        public IReadOnlyList<Product> GetAll()
        {
            lock (_lock)
            {
                // Возвращаем снимок списка (копию),
                // чтобы извне нельзя было изменить внутреннюю коллекцию.
                return _products.ToList();
            }
        }

        public Product? GetBySku(string sku)
        {
            if (string.IsNullOrWhiteSpace(sku))
                return null;

            string normalizedSku = sku.Trim();

            lock (_lock)
            {
                return _products.FirstOrDefault(p =>
                    string.Equals(p.Sku?.Trim(), normalizedSku, StringComparison.OrdinalIgnoreCase));
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _products.Clear();
            }
        }
    }
}