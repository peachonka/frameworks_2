// NotificationModule.cs
using Microsoft.Extensions.DependencyInjection;
using Pr2.ModulesAndDi.Core;
using Pr2.ModulesAndDi.Services;

namespace Pr2.ModulesAndDi.Modules;

public sealed class NotificationModule : IAppModule
{
    public string Name => "Notification";
    public IReadOnlyCollection<string> Requires => new[] { "Core", "Discount" };
    public int ContractVersion => 1;
    
    public void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<INotificationService, EmailNotification>();
        services.AddSingleton<IAppAction, NotificationAction>();
    }
    
    public Task InitializeAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
        => Task.CompletedTask;
    
    private sealed class NotificationAction : IAppAction
    {
        private readonly INotificationService _notifier;
        private readonly IStorage _storage;
        
        public NotificationAction(INotificationService notifier, IStorage storage)
        {
            _notifier = notifier;
            _storage = storage;
        }
        
        public string Title => "Отправка уведомлений";
        
        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("\n=== Отправка уведомлений ===");
            
            await _notifier.SendAsync("✅ Приложение успешно запущено");
            await _notifier.SendAsync($"ℹ️  В хранилище {_storage.GetAll().Count} записей");
            
            if (_storage.GetAll().Count == 0)
            {
                await _notifier.SendAsync("⚠️  Хранилище пусто!");
            }
            
            await _notifier.SendAsync("✅ Операция выполнена");
        }
    }
}

public interface INotificationService
{
    Task SendAsync(string message);
}

public class EmailNotification : INotificationService
{
    public Task SendAsync(string message)
    {
        Console.WriteLine($"Уведомление: {message}");
        return Task.CompletedTask;
    }
}