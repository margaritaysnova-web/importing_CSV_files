using System.Globalization;
using Pr02Ber.Models;

namespace Pr02Ber.Services
{
    public class ProductImportService : IProductImportService
    {
        private readonly IProductRepository _repository;

        private const string DefaultCurrency = "RUB";
        private const string DefaultCategory = "Без категории";
        private const string MsgFileNotFound = "Файл не найден";
        private const string MsgNoData = "Данные для импорта отсутствуют";
        private const string MsgBadHeader = "Неверные заголовки CSV";
        private const string MsgBadFieldsCount = "Неверное количество полей в строке";
        private const string MsgDuplicateSku = "Артикул товара уже существует";

        public ProductImportService(IProductRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public ImportResult ImportFromCsv(string filePath)
        {
            var result = new ImportResult();

            if (string.IsNullOrWhiteSpace(filePath))
            {
                result.Errors.Add(new ImportError { RowNumber = 0, Message = MsgFileNotFound });
                return result;
            }

            if (!File.Exists(filePath))
            {
                result.Errors.Add(new ImportError { RowNumber = 0, Message = MsgFileNotFound });
                return result;
            }

            string[] lines;
            try
            {
                lines = File.ReadAllLines(filePath);
            }
            catch (IOException)
            {
                result.Errors.Add(new ImportError { RowNumber = 0, Message = "Ошибка чтения файла" });
                return result;
            }
            catch (UnauthorizedAccessException)
            {
                result.Errors.Add(new ImportError { RowNumber = 0, Message = "Нет доступа к файлу" });
                return result;
            }

            return ImportFromLines(lines);
        }

        public ImportResult ImportFromLines(IEnumerable<string> lines)
        {
            var result = new ImportResult();
            if (lines == null) return result;

            var listLines = lines.ToList();
            if (listLines.Count == 0 || listLines.All(string.IsNullOrWhiteSpace))
            {
                result.Errors.Add(new ImportError { RowNumber = 0, Message = MsgNoData });
                return result;
            }

            // Ищем заголовок
            int headerIndex = listLines.FindIndex(l => !string.IsNullOrWhiteSpace(l));
            string headerLine = listLines[headerIndex];
            var headers = CsvLineParser.Parse(headerLine);

            // Проверяем обязательные колонки
            var requiredColumns = new[] { "sku", "name", "description", "price", "currency", "category_name", "stock", "image_url", "is_active", "brand", "warranty_months", "model_number" };
            var columnMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < headers.Count; i++)
            {
                var cleanHeader = headers[i].Trim();
                if (!columnMap.ContainsKey(cleanHeader))
                {
                    columnMap[cleanHeader] = i;
                }
            }

            if (requiredColumns.Any(col => !columnMap.ContainsKey(col)))
            {
                result.Errors.Add(new ImportError { RowNumber = headerIndex + 1, Message = MsgBadHeader });
                return result;
            }

            // Обрабатываем строки данных
            var importedSkus = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = headerIndex + 1; i < listLines.Count; i++)
            {
                string line = listLines[i];
                if (string.IsNullOrWhiteSpace(line)) continue; // Пустые строки пропускаем

                result.TotalRows++;
                int rowNumber = i + 1;
                var values = CsvLineParser.Parse(line);

                // Извлечение значений по колонкам
                string GetValue(string col) => columnMap.TryGetValue(col, out int idx) && idx < values.Count ? values[idx] : string.Empty;

                var rowErrors = new List<ImportError>();

                // Валидация полей
                string sku = GetValue("sku").Trim();
                if (string.IsNullOrWhiteSpace(sku))
                    rowErrors.Add(new ImportError { RowNumber = rowNumber, Message = "Поле sku не может быть пустым" });

                string name = GetValue("name").Trim();
                if (string.IsNullOrWhiteSpace(name))
                    rowErrors.Add(new ImportError { RowNumber = rowNumber, Message = "Поле name не может быть пустым" });

                string priceStr = GetValue("price").Trim();
                if (!decimal.TryParse(priceStr, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal price) || price <= 0)
                    rowErrors.Add(new ImportError { RowNumber = rowNumber, Message = "Поле price должно быть числом больше 0" });

                string stockStr = GetValue("stock").Trim();
                if (!int.TryParse(stockStr, out int stock) || stock < 0)
                    rowErrors.Add(new ImportError { RowNumber = rowNumber, Message = "Остаток на складе должен быть целым числом не меньше 0" });

                string isActiveStr = GetValue("is_active").Trim().ToLowerInvariant();
                bool isActive = false;
                bool isActiveValid = isActiveStr == "true" || isActiveStr == "1" || isActiveStr == "false" || isActiveStr == "0";
                if (isActiveValid)
                    isActive = isActiveStr == "true" || isActiveStr == "1";
                else
                    rowErrors.Add(new ImportError { RowNumber = rowNumber, Message = "Поле is_active должно иметь значение true, false, 1 или 0" });

                // Валидация варианта 1
                string brand = GetValue("brand").Trim();
                if (string.IsNullOrWhiteSpace(brand))
                    rowErrors.Add(new ImportError { RowNumber = rowNumber, Message = "Поле brand обязательно»" });

                string warrantyStr = GetValue("warranty_months").Trim();
                if (!int.TryParse(warrantyStr, out int warranty) || warranty < 0 || warranty > 60)
                    rowErrors.Add(new ImportError { RowNumber = rowNumber, Message = "Гарантия должна быть целым числом от 0 до 60 месяцев" });

                // Если есть ошибки — сохраняем и пропускаем строку
                if (rowErrors.Count > 0)
                {
                    result.Errors.AddRange(rowErrors);
                    continue;
                }

                // Проверка дубликата SKU
                bool isDuplicate = _repository.GetBySku(sku) != null || importedSkus.Contains(sku);
                if (isDuplicate)
                {
                    result.Errors.Add(new ImportError { RowNumber = rowNumber, Message = MsgDuplicateSku });
                    continue;
                }

                // Создание объекта Product
                var product = new Product
                {
                    Sku = sku,
                    Name = name,
                    Description = GetValue("description").Trim(),
                    Price = price,
                    Currency = NormalizeCurrency(GetValue("currency")),
                    CategoryName = NormalizeCategory(GetValue("category_name")),
                    StockQuantity = stock,
                    ImageUrl = GetValue("image_url").Trim(),
                    IsActive = isActive,
                    Brand = brand,
                    WarrantyMonths = warranty,
                    ModelNumber = GetValue("model_number").Trim()
                };

                // Добавление в репозиторий
                _repository.Add(product);
                importedSkus.Add(sku);
                result.ImportedCount++;
            }

            if (result.TotalRows == 0 && result.Errors.Count == 0)
            {
                result.Errors.Add(new ImportError { RowNumber = 0, Message = MsgNoData });
            }

            return result;
        }

        private static string NormalizeCurrency(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return DefaultCurrency;
            return value.Trim().ToUpperInvariant();
        }

        private static string NormalizeCategory(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return DefaultCategory;
            return value.Trim();
        }
    }
}