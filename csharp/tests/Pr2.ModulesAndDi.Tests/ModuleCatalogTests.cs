using Microsoft.Extensions.DependencyInjection;
using Pr2.ModulesAndDi.Core;
using Pr2.ModulesAndDi.Modules;
using Pr2.ModulesAndDi.Services;
using Xunit;

namespace Pr2.ModulesAndDi.Tests;

public sealed class NewModulesTests
{

    static NewModulesTests()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.InputEncoding = System.Text.Encoding.UTF8;
    }

    [Fact]
    public async Task CurrencyModule_КонвертируетВалюты()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IStorage, InMemoryStorage>();
        services.AddSingleton<IClock, SystemClock>();
        
        var module = new CurrencyModule();
        module.RegisterServices(services);
        
        var provider = services.BuildServiceProvider();
        var action = provider.GetServices<IAppAction>().First(a => a.Title.Contains("Конвертация"));
        
        // Act
        await action.ExecuteAsync(CancellationToken.None);
        
        // Assert
        var storage = provider.GetService<IStorage>();
        Assert.Contains(storage!.GetAll(), item => item.Contains("USD"));
    }

    [Fact]
    public async Task DiscountModule_ПрименяетСкидку()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IStorage, InMemoryStorage>();
        services.AddSingleton<IClock, SystemClock>();
        
        var module = new DiscountModule();
        module.RegisterServices(services);
        
        var provider = services.BuildServiceProvider();
        var action = provider.GetServices<IAppAction>().First(a => a.Title.Contains("скидок"));
        
        // Act
        await action.ExecuteAsync(CancellationToken.None);
        
        // Assert
        var storage = provider.GetService<IStorage>();
        var records = storage!.GetAll();
        Assert.Contains(records, r => r.Contains("скидк"));
        Assert.Contains(records, r => r.Contains("1000"));
    }

    [Fact]
    public async Task FileLoggingModule_СоздаётФайлЛога()
    {
        // Arrange
        var services = new ServiceCollection();
        var storage = new InMemoryStorage();
        storage.Add("Тестовые данные");
        services.AddSingleton<IStorage>(storage);
        services.AddSingleton<IClock, SystemClock>();
        
        var module = new FileLoggingModule();
        module.RegisterServices(services);
        
        var provider = services.BuildServiceProvider();
        var action = provider.GetServices<IAppAction>().First(a => a.Title.Contains("логирование"));
        
        // Путь, куда модуль будет писать (должен совпадать с логикой в FileLoggingModule)
        var logDir = Path.Combine(AppContext.BaseDirectory, "logs");
        var expectedFileName = $"app-{DateTime.Now:yyyy-MM-dd}.log";
        var expectedFilePath = Path.Combine(logDir, expectedFileName);
        
        // Не удаляем папку заранее — модуль сам создаст её при необходимости
        // Это избежит блокировки файла, если он ещё открыт
        
        // Act
        await action.ExecuteAsync(CancellationToken.None);
        
        // Важно: ждём, пока асинхронная запись в файл реально завершится
        // В модуле есть FlushAsync(), но на всякий случай добавляем небольшую задержку
        await Task.Delay(200);
        
        // Assert
        // 1. Папка должна существовать
        Assert.True(Directory.Exists(logDir), $"Папка логов не создана: {logDir}");
        
        // 2. Ожидаемый файл должен существовать
        Assert.True(File.Exists(expectedFilePath), 
            $"Файл лога не найден: {expectedFilePath}. " +
            $"Файлы в папке: {string.Join(", ", Directory.GetFiles(logDir, "*", SearchOption.TopDirectoryOnly))}");
        
        // 3. Файл не должен быть пустым
        var content = await File.ReadAllTextAsync(expectedFilePath);
        Assert.NotEmpty(content);
        
        // 4. Файл должен содержать ожидаемые записи
        Assert.Contains("INFO", content);
        Assert.Contains("Приложение запущено", content);
        Assert.Contains("Тестовая ошибка", content);
    }

    [Fact]
    public async Task NotificationModule_ОтправляетУведомления()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IStorage, InMemoryStorage>();
        services.AddSingleton<IClock, SystemClock>();
        
        var module = new NotificationModule();
        module.RegisterServices(services);
        
        var provider = services.BuildServiceProvider();
        var action = provider.GetServices<IAppAction>().First(a => a.Title.Contains("уведомлений"));
        
        // Act & Assert (просто проверяем, что не падает)
        var exception = Record.Exception(() => action.ExecuteAsync(CancellationToken.None).Wait());
        Assert.Null(exception);
    }

    [Fact]
    public async Task AnalyticsModule_СобираетСтатистику()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IStorage, InMemoryStorage>();
        services.AddSingleton<IClock, SystemClock>();
        
        var module = new AnalyticsModule();
        module.RegisterServices(services);
        
        var provider = services.BuildServiceProvider();
        var action = provider.GetServices<IAppAction>().First(a => a.Title.Contains("аналитики"));
        
        // Act
        await action.ExecuteAsync(CancellationToken.None);
        
        // Assert (проверяем, что что-то вывелось в консоль - перехватываем)
        using var sw = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(sw);
        
        await action.ExecuteAsync(CancellationToken.None);
        var output = sw.ToString();
        
        Console.SetOut(originalOut);
        Assert.Contains("Событие", output);
    }

    [Fact]
    public async Task Модули_МогутРаботатьВместе()
    {
        // Arrange - регистрируем все модули
        var services = new ServiceCollection();
        services.AddSingleton<IStorage, InMemoryStorage>();
        services.AddSingleton<IClock, SystemClock>();
        
        var modules = new IAppModule[]
        {
            new CoreModule(),
            new CurrencyModule(),
            new DiscountModule(),
            new FileLoggingModule(),
            new NotificationModule()
        };
        
        foreach (var module in modules)
        {
            module.RegisterServices(services);
        }
        
        var provider = services.BuildServiceProvider();
        var actions = provider.GetServices<IAppAction>().ToList();
        
        // Act & Assert - все действия должны выполниться без ошибок
        foreach (var action in actions)
        {
            var exception = Record.Exception(() => action.ExecuteAsync(CancellationToken.None).Wait());
            Assert.Null(exception);
        }
    }

    [Fact]
    public async Task Модуль_ТребуетCore_ЕслиCoreОтсутствует_Ошибка()
    {
        // Arrange
        var all = new Dictionary<string, IAppModule>(StringComparer.OrdinalIgnoreCase)
        {
            ["Currency"] = new CurrencyModule()  // требует Core, но Core нет
        };
        
        // Act & Assert
        var ex = Assert.Throws<ModuleLoadException>(() => 
            ModuleCatalog.BuildExecutionOrder(all, new[] { "Currency" }));
        
        Assert.Contains("Не хватает модуля", ex.Message);
        Assert.Contains("Currency требует Core", ex.Message);
    }
}