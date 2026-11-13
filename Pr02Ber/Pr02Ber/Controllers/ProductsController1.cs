using Microsoft.AspNetCore.Mvc;
using Pr02Ber.Models;
using Pr02Ber.Services;

namespace Pr02Ber.Controllers
{
    public class ProductsController : Controller
    {
        private readonly IProductRepository _repository;
        private readonly IProductImportService _importService;
        private readonly IWebHostEnvironment _environment;

        // Внедрение зависимостей (DI) через конструктор
        public ProductsController(
            IProductRepository repository,
            IProductImportService importService,
            IWebHostEnvironment environment)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _importService = importService ?? throw new ArgumentNullException(nameof(importService));
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        }

        // Страница со списком товаров
        [HttpGet]
        public ActionResult Index()
        {
            var viewModel = new ProductsIndexViewModel
            {
                Products = _repository.GetAll()
            };
            return View(viewModel);
        }

        // Страница импорта (открывается при переходе по ссылке)
        [HttpGet]
        public ActionResult Import()
        {
            return View(new ImportViewModel());
        }

        // Обработка формы импорта (нажатие кнопки "Импортировать")
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Import(string filePath)
        {
            var resolvedPath = ResolveFilePath(filePath);

            // Если путь пустой или не удалось его преобразовать
            if (string.IsNullOrWhiteSpace(resolvedPath))
            {
                var emptyModel = new ImportViewModel();
                emptyModel.Result = new ImportResult();
                emptyModel.Result.Errors.Add(new ImportError { RowNumber = 0, Message = "Файл не найден" });
                return View("Import", emptyModel);
            }

            // Запускаем импорт
            var result = _importService.ImportFromCsv(resolvedPath);

            var viewModel = new ImportViewModel
            {
                FilePath = filePath, // Сохраняем введенный путь, чтобы он остался в текстовом поле
                Result = result
            };

            return View("Import", viewModel);
        }

        // Вспомогательный метод для определения физического пути к файлу
        private string? ResolveFilePath(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return null;

            filePath = filePath.Trim();

            // Если это полный путь (например, C:\data\file.csv) и файл существует
            if (Path.IsPathRooted(filePath) && System.IO.File.Exists(filePath))
            {
                return filePath;
            }

            // Если путь относительный (например, /csv/sample_products.csv)
            // Убираем ведущий слеш, если он есть
            var relativePath = filePath.TrimStart('/', '\\');

            // Получаем физический путь к папке wwwroot
            var webRoot = _environment.WebRootPath;
            if (string.IsNullOrWhiteSpace(webRoot))
            {
                webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            }

            // Склеиваем путь (заменяем прямые слеши на системные)
            var fullPath = Path.Combine(webRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));

            return fullPath;
        }
    }
}