# Учёт расходов (C#, WinForms)

Небольшое десктоп-приложение для Windows: записывайте траты, смотрите итоги по месяцам и категориям.

## Возможности
- Добавление расхода: дата, категория, сумма, заметка
- Свои категории (просто впишите новое название)
- Фильтр по месяцу или «за всё время»
- Итог и разбивка по категориям с долями в процентах
- Удаление записи (кнопка или клавиша Delete)
- Экспорт в CSV, открывается в Excel
- Данные сохраняются автоматически в `%AppData%\ExpenseTracker\expenses.json`

## Запуск
Нужны Windows и [.NET 8 SDK](https://dotnet.microsoft.com/download).

```
dotnet run
```

Сборка одного exe-файла:

```
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
```

Результат лежит в `bin/Release/net8.0-windows/win-x64/publish/`.
Валюта меняется константой `Currency` в `MainForm.cs`.

## Структура
- `Program.cs` — точка входа
- `MainForm.cs` — окно и интерфейс
- `ExpenseStore.cs` — хранение в JSON и экспорт CSV
- `Expense.cs` — модель записи

Стек: C#, .NET 8, Windows Forms.
