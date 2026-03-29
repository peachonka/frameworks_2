using Microsoft.Extensions.DependencyInjection;
using Pr2.ModulesAndDi.Core;
using Pr2.ModulesAndDi.Services;
using System.Text.Json;

namespace Pr2.ModulesAndDi.Modules;

public sealed class AnalyticsModule : IAppModule
{
    public string Name => "Analytics";
    public IReadOnlyCollection<string> Requires => new[] { "Core" };
    public int ContractVersion => 1;
    
    public void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<IAppAction, AnalyticsAction>();
    }
    
    public Task InitializeAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
        => Task.CompletedTask;
    
    private sealed class AnalyticsAction : IAppAction
    {
        private readonly IStorage _storage;
        private readonly List<(string name, object data, DateTime time)> _events = new();
        
        public AnalyticsAction(IStorage storage)
        {
            _storage = storage;
        }
        
        public string Title => "Сбор аналитики";
        
        public Task ExecuteAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("\n=== Сбор аналитики ===");
            
            TrackEvent("app_start", new { version = "1.0" });
            TrackEvent("storage_access", new { operation = "get", count = _storage.GetAll().Count });
            
            var byName = _events.GroupBy(e => e.name).ToDictionary(g => g.Key, g => g.Count());
            Console.WriteLine($"Статистика: {_events.Count} событий");
            Console.WriteLine($"События по типам: {JsonSerializer.Serialize(byName)}");
            
            return Task.CompletedTask;
        }
        
        private void TrackEvent(string name, object data)
        {
            _events.Add((name, data, DateTime.Now));
            Console.WriteLine($"[Analytics] Событие: {name} {JsonSerializer.Serialize(data)}");
        }
    }
}