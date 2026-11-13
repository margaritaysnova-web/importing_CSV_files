using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pr02Ber.Models;
using Pr02Ber.Services;

namespace Pr02Ber.Tests
{
    public class ProductImportServiceTests
    {
        private const string Header = "sku,name,description,price,currency,category_name,stock,image_url,is_active,brand,warranty_months,model_number";

        [Fact]
        public void Import_Successful_SingleRow()
        {
            var repo = new InMemoryProductRepository();
            var service = new ProductImportService(repo);
            var lines = new[]
            {
                Header,
                "1001,Ноутбук Lenovo IdeaPad 3,Бюджетный ноутбук,45990,RUB,Компьютеры,5,/images/laptop.jpg,true,Lenovo,24,IdeaPad 3-15"
            };

            var result = service.ImportFromLines(lines);

            Assert.Equal(1, result.TotalRows);
            Assert.Equal(1, result.ImportedCount);
            Assert.Empty(result.Errors);
            var p = repo.GetBySku("1001");
            Assert.NotNull(p);
            Assert.Equal("Lenovo", p.Brand);
            Assert.Equal(24, p.WarrantyMonths);
        }

        [Fact]
        public void Import_FileNotFound_ReturnsError()
        {
            var service = new ProductImportService(new InMemoryProductRepository());
            var result = service.ImportFromCsv("C:\\nonexistent_file_123.csv");

            Assert.Equal(0, result.TotalRows);
            Assert.Single(result.Errors);
            Assert.Contains("Файл не найден", result.Errors[0].Message);
        }

        [Fact]
        public void Import_EmptyLines_ReturnsError()
        {
            var service = new ProductImportService(new InMemoryProductRepository());
            var result = service.ImportFromLines(new List<string>());

            Assert.Equal(0, result.TotalRows);
            Assert.Single(result.Errors);
            Assert.Contains("Данные для импорта отсутствуют", result.Errors[0].Message);
        }

        [Fact]
        public void Import_BadHeader_ReturnsError()
        {
            var service = new ProductImportService(new InMemoryProductRepository());
            var lines = new[] { "sku,name,description,price" };

            var result = service.ImportFromLines(lines);

            Assert.Single(result.Errors);
            Assert.Contains("Неверные заголовки CSV", result.Errors[0].Message);
        }

        [Fact]
        public void Import_InvalidPrice_ReturnsError()
        {
            var service = new ProductImportService(new InMemoryProductRepository());
            var lines = new[] { Header, "1003,Клава,, -500,RUB,Периферия,7,/img.jpg,true,A4Tech,24,V3M" };

            var result = service.ImportFromLines(lines);

            Assert.Equal(0, result.ImportedCount);
            Assert.Single(result.Errors);
            Assert.Contains("Поле price должно быть числом больше 0", result.Errors[0].Message);
        }

        [Fact]
        public void Import_MissingBrand_ReturnsError()
        {
            var service = new ProductImportService(new InMemoryProductRepository());
            var lines = new[] { Header, "1005,Видеокарта,,24990,RUB,Компьютеры,4,/img.jpg,true,,36,RTX3060" };

            var result = service.ImportFromLines(lines);

            Assert.Equal(0, result.ImportedCount);
            Assert.Contains(result.Errors, e => e.Message.Contains("Поле brand обязательно"));
        }

        [Fact]
        public void Import_WarrantyOutOfRange_ReturnsError()
        {
            var service = new ProductImportService(new InMemoryProductRepository());
            var lines = new[] { Header, "1006,SSD,,5990,RUB,Компьютеры,12,/img.jpg,true,Samsung,61,870EVO" };

            var result = service.ImportFromLines(lines);

            Assert.Equal(0, result.ImportedCount);
            Assert.Contains(result.Errors, e => e.Message.Contains("Гарантия должна быть целым числом от 0 до 60"));
        }

        [Fact]
        public void Import_DuplicateSkuInFile_SkipsSecond()
        {
            var service = new ProductImportService(new InMemoryProductRepository());
            var lines = new[]
            {
                Header,
                "1001,Ноутбук,,45990,RUB,Компьютеры,5,,true,Lenovo,24,IdeaPad",
                "1001,Ноутбук Асус,,52990,RUB,Компьютеры,7,,true,ASUS,24,VivoBook"
            };

            var result = service.ImportFromLines(lines);

            Assert.Equal(2, result.TotalRows);
            Assert.Equal(1, result.ImportedCount);
            Assert.Single(result.Errors);
            Assert.Contains("Артикул товара уже существует", result.Errors[0].Message);
        }

        [Fact]
        public void Import_EmptyCurrencyAndCategory_AppliesDefaults()
        {
            var repo = new InMemoryProductRepository();
            var service = new ProductImportService(repo);
            var lines = new[] { Header, "1011,Блок питания,,4990,,,9,,true,Corsair,60,CV550" };

            var result = service.ImportFromLines(lines);

            Assert.Equal(1, result.ImportedCount);
            var p = repo.GetBySku("1011");
            Assert.Equal("RUB", p.Currency);
            Assert.Equal("Без категории", p.CategoryName);
        }

        [Fact]
        public void Import_EmptyOptionalFields_Allowed()
        {
            var repo = new InMemoryProductRepository();
            var service = new ProductImportService(repo);
            // Пустые description и image_url, и model_number
            var lines = new[] { Header, "1011,Блок питания,,4990,RUB,Компьютеры,9,,true,Corsair,60," };

            var result = service.ImportFromLines(lines);

            Assert.Equal(1, result.ImportedCount);
            Assert.Empty(result.Errors);
        }
    }
}