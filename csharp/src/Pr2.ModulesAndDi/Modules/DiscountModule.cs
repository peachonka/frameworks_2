using Microsoft.Extensions.DependencyInjection;
using Pr2.ModulesAndDi.Core;
using Pr2.ModulesAndDi.Services;

namespace Pr2.ModulesAndDi.Modules;

public sealed class DiscountModule : IAppModule
{
    public string Name => "Discount";
    public IReadOnlyCollection<string> Requires => new[] { "Core" };
    public int ContractVersion => 1;
    
    public void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<IAppAction, DiscountAction>();
    }
    
    public Task InitializeAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
        => Task.CompletedTask;
    
    private sealed class DiscountAction : IAppAction
    {
        private readonly IStorage _storage;
        
        // Стратегии скидок
        private readonly Dictionary<string, Func<decimal, decimal>> _strategies = new()
        {
            ["seasonal"] = amount => amount * 0.9m,      // 10% сезонная
            ["blackFriday"] = amount => amount * 0.7m,   // 30% чёрная пятница
            ["newYear"] = amount => amount * 0.85m,      // 15% новогодняя
            ["vip"] = amount => amount * 0.8m,           // 20% для VIP
            ["none"] = amount => amount
        };
        
        private string _currentStrategy = "none";
        
        public DiscountAction(IStorage storage)
        {
            _storage = storage;
        }
        
        public string Title => "Расчёт скидок";
        
        public Task ExecuteAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("\n=== Расчёт скидок ===");
            
            const decimal amount = 1000;
            Console.WriteLine($"Сумма заказа: {amount} ₽");
            
            // Применяем разные скидки
            var strategiesToShow = new[] { "seasonal", "blackFriday", "newYear", "vip" };
            
            foreach (var strategy in strategiesToShow)
            {
                SetStrategy(strategy);
                var result = ApplyDiscount(amount);
                
                Console.WriteLine($"{strategy}: {result.Discounted} ₽ (скидка {result.Percent}%, экономия {result.Saved} ₽)");
                
                _storage.Add($"Заказ {amount}₽ со скидкой {strategy}: {result.Discounted}₽");
            }
            
            // Возвращаем обычную цену
            SetStrategy("none");
            var normal = ApplyDiscount(amount);
            Console.WriteLine($"Обычная цена: {normal.Discounted} ₽");
            
            return Task.CompletedTask;
        }
        
        private void SetStrategy(string strategy)
        {
            if (_strategies.ContainsKey(strategy))
            {
                _currentStrategy = strategy;
                Console.WriteLine($"[Discount] Установлена стратегия: {strategy}");
            }
            else
            {
                Console.WriteLine($"[Discount] Стратегия {strategy} не найдена, используем none");
                _currentStrategy = "none";
            }
        }
        
        private DiscountResult ApplyDiscount(decimal amount)
        {
            var discounted = _strategies[_currentStrategy](amount);
            var saved = amount - discounted;
            var percent = (int)Math.Round((saved / amount) * 100);
            
            return new DiscountResult
            {
                Original = amount,
                Discounted = Math.Round(discounted, 2),
                Saved = Math.Round(saved, 2),
                Percent = percent,
                Strategy = _currentStrategy
            };
        }
        
        private class DiscountResult
        {
            public decimal Original { get; set; }
            public decimal Discounted { get; set; }
            public decimal Saved { get; set; }
            public int Percent { get; set; }
            public string Strategy { get; set; } = "";
        }
    }
}