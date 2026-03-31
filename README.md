# Практическая работа №2

## Описание проекта

Проект реализует модульную архитектуру с использованием внедрения зависимостей (DI). Приложение можно расширять модулями без изменения ядра. Модули загружаются автоматически.

### Конфигурация модулей

```json
// appsettings.json
{
  "Modules": [
    "Core",
    "Currency",
    "Discount",
    "FileLogging",
    "Notification",
    "Analytics"
  ]
}
```

## Запуск

### Требования

- .NET 8.0 SDK
- Visual Studio 2022 / VS Code / Rider

### Сборка и запуск

```bash
# Перейти в папку с проектом
cd csharp/src/Pr2.ModulesAndDi

# Сборка
dotnet build

# Запуск
dotnet run
```


### Запуск тестов

```bash
cd csharp/tests/Pr2.ModulesAndDi.Tests
dotnet test
```
