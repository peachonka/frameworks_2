using Microsoft.Extensions.DependencyInjection;
using Pr2.ModulesAndDi.Core;
using Pr2.ModulesAndDi.Services;

namespace Pr2.ModulesAndDi.Modules;

public sealed class CurrencyModule : IAppModule
{
    public string Name => "Currency";
    public IReadOnlyCollection<string> Requires => new[] { "Core" };
    public int ContractVersion => 1;
    
    public void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<IAppAction, CurrencyAction>();
    }
    
    public Task InitializeAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
        => Task.CompletedTask;
    
    private sealed class CurrencyAction : IAppAction
    {
        private readonly IStorage _storage;
        
        // Курсы валют
        private readonly Dictionary<string, decimal> _rates = new()
        {
            ["USD"] = 1m,
            ["EUR"] = 0.92m,
            ["RUB"] = 85.5m,
            ["KZT"] = 470m
        };
        
        private readonly Dictionary<string, string> _symbols = new()
        {
            ["USD"] = "$",
            ["EUR"] = "€",
            ["RUB"] = "₽",
            ["KZT"] = "₸"
        };
        
        public CurrencyAction(IStorage storage)
        {
            _storage = storage;
        }
        
        public string Title => "Конвертация валют";
        
        public Task ExecuteAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("\n=== Конвертация валют ===");
            
            const decimal usdPrice = 100;
            var rubPrice = Convert(usdPrice, "USD", "RUB");
            var eurPrice = Convert(usdPrice, "USD", "EUR");
            
            Console.WriteLine($"100 USD = {Format(rubPrice, "RUB")}");
            Console.WriteLine($"100 USD = {Format(eurPrice, "EUR")}");
            
            _storage.Add($"Курс USD: {rubPrice} RUB");
            Console.WriteLine("Курс сохранён в хранилище");
            
            return Task.CompletedTask;
        }
        
        private decimal Convert(decimal amount, string from, string to)
        {
            var inUSD = amount / _rates[from];
            var result = inUSD * _rates[to];
            return Math.Round(result, 2);
        }
        
        private string Format(decimal amount, string currency)
        {
            return $"{amount:F2} {_symbols[currency]}";
        }
    }
}