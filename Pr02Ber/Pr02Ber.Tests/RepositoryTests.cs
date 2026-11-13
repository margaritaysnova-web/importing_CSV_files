using Pr02Ber.Models;
using Pr02Ber.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pr02Ber.Tests
{
    public class RepositoryTests
    {
        // Тест 1: Добавление товара
        [Fact]
        public void Add_AddsProduct_GetAllContainsIt()
        {
            var repository = new InMemoryProductRepository();
            var product = new Product
            {
                Sku = "1001",
                Name = "Ноутбук Lenovo IdeaPad 3",
                Price = 45990m,
                Brand = "Lenovo"
            };

            repository.Add(product);
            var all = repository.GetAll();

            Assert.Single(all);
            Assert.Equal("1001", all[0].Sku);
            Assert.Equal(1, repository.Count);
        }

        // Тест 2: Поиск по SKU (точное совпадение)
        [Fact]
        public void GetBySku_ReturnsProduct_WhenSkuExists()
        {
            var repository = new InMemoryProductRepository();
            var product = new Product { Sku = "1001", Name = "Товар 1" };
            repository.Add(product);

            var found = repository.GetBySku("1001");

            Assert.NotNull(found);
            Assert.Equal("1001", found.Sku);
        }

        // Тест 3: Поиск по SKU без учёта регистра и пробелов
        [Fact]
        public void GetBySku_IgnoresCaseAndSpaces()
        {
            var repository = new InMemoryProductRepository();
            var product = new Product { Sku = "1001", Name = "Товар 1" };
            repository.Add(product);

            var found = repository.GetBySku(" 1001 ");
            var found2 = repository.GetBySku(" 1001"); // другой набор пробелов

            Assert.NotNull(found);
            Assert.NotNull(found2);
            Assert.Equal("1001", found.Sku);
        }

        // Тест 4: Пустой репозиторий
        [Fact]
        public void EmptyRepository_ReturnsZeroAndNull()
        {
            var repository = new InMemoryProductRepository();

            Assert.Equal(0, repository.Count);
            Assert.Empty(repository.GetAll());
            Assert.Null(repository.GetBySku("1001"));
        }
    }
}