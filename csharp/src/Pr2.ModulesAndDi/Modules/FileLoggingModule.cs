using Microsoft.Extensions.DependencyInjection;
using Pr2.ModulesAndDi.Core;
using Pr2.ModulesAndDi.Services;

namespace Pr2.ModulesAndDi.Modules;

public sealed class FileLoggingModule : IAppModule
{
    public string Name => "FileLogging";
    public IReadOnlyCollection<string> Requires => new[] { "Core" };
    public int ContractVersion => 1;
    
    public void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<IAppAction, FileLoggingAction>();
    }
    
    public Task InitializeAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken)
        => Task.CompletedTask;
    
    private sealed class FileLoggingAction : IAppAction
    {
        private readonly IStorage _storage;
        private readonly string _logFile;
        private readonly List<string> _buffer = new();
        
        public FileLoggingAction(IStorage storage)
        {
            _storage = storage;
            var logDir = Path.Combine(AppContext.BaseDirectory, "logs");
            Directory.CreateDirectory(logDir);
            _logFile = Path.Combine(logDir, $"app-{DateTime.Now:yyyy-MM-dd}.log");
        }
        
        public string Title => "Файловое логирование";
        
        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("\n=== Файловое логирование ===");
            
            await Log("INFO", "Приложение запущено");
            await Log("INFO", $"В хранилище {_storage.GetAll().Count} записей");
            
            var data = _storage.GetAll();
            if (data.Count > 0)
            {
                await Log("INFO", $"Содержимое хранилища: {string.Join(", ", data)}");
            }
            
            await Log("WARN", "Это предупреждение");
            await Log("ERROR", "Тестовая ошибка");
            await Log("INFO", "Логирование завершено");
            
            await FlushAsync();
            Console.WriteLine($"Логи сохранены в {_logFile}");
        }
        
        private Task Log(string level, string message)
        {
            var entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}";
            Console.WriteLine(entry);
            
            lock (_buffer)
            {
                _buffer.Add(entry);
            }
            
            return Task.CompletedTask;
        }
        
        private async Task FlushAsync()
        {
            string[] toWrite;
            lock (_buffer)
            {
                if (_buffer.Count == 0) return;
                toWrite = _buffer.ToArray();
                _buffer.Clear();
            }
            
            await File.AppendAllLinesAsync(_logFile, toWrite);
        }
    }
}