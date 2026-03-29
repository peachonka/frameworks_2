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
        services.AddSingleton<IStorage, InMemoryStorage>();
        services.AddSingleton<IClock, SystemClock>();
        
        var storage = new InMemoryStorage();
        storage.Add("Тестовые данные");
        services.AddSingleton<IStorage>(storage);
        
        var module = new FileLoggingModule();
        module.RegisterServices(services);
        
        var provider = services.BuildServiceProvider();
        var action = provider.GetServices<IAppAction>().First(a => a.Title.Contains("логирование"));
        
        var logPath = Path.Combine(AppContext.BaseDirectory, "logs");
        if (Directory.Exists(logPath))
            Directory.Delete(logPath, true);
           
           
        Directory.CreateDirectory(logPath);
        // Act
        await action.ExecuteAsync(CancellationToken.None);
        
        // Assert
        Assert.True(Directory.Exists(logPath));
        Assert.True(Directory.GetFiles(logPath).Length > 0);
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