using System.Text;
using System.Text.Json;

namespace ExpenseTracker;

/// <summary>Хранит расходы в JSON-файле в папке %AppData%\ExpenseTracker.</summary>
public class ExpenseStore
{
    private readonly string _path;

    public List<Expense> Items { get; private set; } = new();

    public ExpenseStore()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ExpenseTracker");
        Directory.CreateDirectory(dir);
        _path = Path.Combine(dir, "expenses.json");
        Load();
    }

    private void Load()
    {
        if (!File.Exists(_path)) return;
        try
        {
            Items = JsonSerializer.Deserialize<List<Expense>>(File.ReadAllText(_path)) ?? new();
        }
        catch (JsonException)
        {
            // Файл повреждён: сохраняем копию, чтобы не потерять данные.
            File.Copy(_path, _path + ".bak", true);
            Items = new();
        }
    }

    public void Save() =>
        File.WriteAllText(_path, JsonSerializer.Serialize(Items, new JsonSerializerOptions { WriteIndented = true }));

    public void Add(Expense e)
    {
        Items.Add(e);
        Save();
    }

    public void Remove(Guid id)
    {
        Items.RemoveAll(x => x.Id == id);
        Save();
    }

    public static void ExportCsv(string path, IEnumerable<Expense> items)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Дата;Категория;Сумма;Заметка");
        foreach (var e in items)
        {
            var note = "\"" + e.Note.Replace("\"", "\"\"") + "\"";
            sb.AppendLine($"{e.Date:yyyy-MM-dd};{e.Category};{e.Amount:0.00};{note}");
        }
        // BOM нужен, чтобы Excel правильно прочитал кириллицу.
        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
    }
}
