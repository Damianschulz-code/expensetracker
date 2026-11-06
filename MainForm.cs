namespace ExpenseTracker;

public class MainForm : Form
{
    private const string Currency = "лей";
    private static readonly string[] DefaultCategories =
        { "Еда", "Транспорт", "Жильё", "Здоровье", "Развлечения", "Покупки", "Другое" };

    private sealed record RowView(Guid Id, string Date, string Category, string Amount, string Note);

    private readonly ExpenseStore _store = new();

    private readonly DateTimePicker _date = new() { Format = DateTimePickerFormat.Short, Width = 110 };
    private readonly ComboBox _category = new() { Width = 150 };
    private readonly NumericUpDown _amount = new()
        { DecimalPlaces = 2, Minimum = 0, Maximum = 10_000_000, Width = 110, ThousandsSeparator = true };
    private readonly TextBox _note = new() { Width = 230, PlaceholderText = "Заметка (необязательно)" };
    private readonly Button _add = new() { Text = "Добавить", AutoSize = true };

    private readonly DateTimePicker _month = new()
        { Format = DateTimePickerFormat.Custom, CustomFormat = "MMMM yyyy", ShowUpDown = true, Width = 160 };
    private readonly CheckBox _all = new() { Text = "За всё время", AutoSize = true };

    private readonly DataGridView _grid = new();
    private readonly ListView _summary = new();
    private readonly Label _totalLabel = new() { AutoSize = true };

    public MainForm()
    {
        Text = "Учёт расходов";
        MinimumSize = new Size(900, 540);
        Size = new Size(1000, 620);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10f);
        AcceptButton = _add;

        var addPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, Padding = new Padding(8) };
        addPanel.Controls.AddRange(new Control[]
            { Lbl("Дата"), _date, Lbl("Категория"), _category, Lbl("Сумма"), _amount, _note, _add });

        var filterPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, Padding = new Padding(8, 0, 8, 0) };
        filterPanel.Controls.AddRange(new Control[] { Lbl("Показать:"), _month, _all });

        SetupGrid();
        SetupSummary();
        var middle = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Padding = new Padding(8) };
        middle.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 66));
        middle.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34));
        middle.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        middle.Controls.Add(_grid, 0, 0);
        middle.Controls.Add(_summary, 1, 0);

        _totalLabel.Font = new Font(Font, FontStyle.Bold);
        _totalLabel.Margin = new Padding(3, 8, 24, 0);
        var delete = new Button { Text = "Удалить выбранное", AutoSize = true };
        var export = new Button { Text = "Экспорт в CSV", AutoSize = true };
        var bottom = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, Padding = new Padding(8) };
        bottom.Controls.AddRange(new Control[] { _totalLabel, delete, export });

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4 };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.Controls.Add(addPanel, 0, 0);
        root.Controls.Add(filterPanel, 0, 1);
        root.Controls.Add(middle, 0, 2);
        root.Controls.Add(bottom, 0, 3);
        Controls.Add(root);

        _add.Click += (_, _) => AddExpense();
        delete.Click += (_, _) => DeleteSelected();
        export.Click += (_, _) => Export();
        _month.ValueChanged += (_, _) => RefreshView();
        _all.CheckedChanged += (_, _) => { _month.Enabled = !_all.Checked; RefreshView(); };
        _grid.KeyDown += (_, e) => { if (e.KeyCode == Keys.Delete) DeleteSelected(); };

        UpdateCategories();
        _category.Text = DefaultCategories[0];
        RefreshView();
    }

    private static Label Lbl(string text) =>
        new() { Text = text, AutoSize = true, Margin = new Padding(8, 8, 3, 0) };

    private void SetupGrid()
    {
        _grid.Dock = DockStyle.Fill;
        _grid.AutoGenerateColumns = false;
        _grid.ReadOnly = true;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.MultiSelect = false;
        _grid.RowHeadersVisible = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.BackgroundColor = SystemColors.Window;

        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Дата", DataPropertyName = nameof(RowView.Date), Width = 100 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Категория", DataPropertyName = nameof(RowView.Category), Width = 140 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Сумма",
            DataPropertyName = nameof(RowView.Amount),
            Width = 110,
            DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight }
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Заметка",
            DataPropertyName = nameof(RowView.Note),
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        });
    }

    private void SetupSummary()
    {
        _summary.Dock = DockStyle.Fill;
        _summary.View = View.Details;
        _summary.FullRowSelect = true;
        _summary.GridLines = true;
        _summary.Columns.Add("Категория", 130);
        _summary.Columns.Add("Сумма", 90, HorizontalAlignment.Right);
        _summary.Columns.Add("Доля", 60, HorizontalAlignment.Right);
    }

    private IEnumerable<Expense> Filtered() =>
        _all.Checked
            ? _store.Items
            : _store.Items.Where(e => e.Date.Year == _month.Value.Year && e.Date.Month == _month.Value.Month);

    private void UpdateCategories()
    {
        var text = _category.Text;
        _category.Items.Clear();
        _category.Items.AddRange(DefaultCategories
            .Union(_store.Items.Select(e => e.Category))
            .OrderBy(c => c)
            .Cast<object>().ToArray());
        _category.Text = text;
    }

    private void RefreshView()
    {
        var items = Filtered().OrderByDescending(e => e.Date).ThenByDescending(e => e.Amount).ToList();
        _grid.DataSource = items
            .Select(e => new RowView(e.Id, e.Date.ToString("dd.MM.yyyy"), e.Category, e.Amount.ToString("N2"), e.Note))
            .ToList();

        var total = items.Sum(e => e.Amount);
        _totalLabel.Text = $"Итого: {total:N2} {Currency}   Записей: {items.Count}";

        _summary.Items.Clear();
        var groups = items.GroupBy(e => e.Category)
            .Select(g => (Name: g.Key, Sum: g.Sum(x => x.Amount)))
            .OrderByDescending(g => g.Sum);
        foreach (var g in groups)
        {
            var share = total == 0 ? 0m : g.Sum / total;
            var li = new ListViewItem(g.Name);
            li.SubItems.Add(g.Sum.ToString("N2"));
            li.SubItems.Add(share.ToString("P0"));
            _summary.Items.Add(li);
        }
    }

    private void AddExpense()
    {
        if (_amount.Value <= 0)
        {
            MessageBox.Show(this, "Введите сумму больше нуля.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            _amount.Focus();
            return;
        }

        var category = string.IsNullOrWhiteSpace(_category.Text) ? "Другое" : _category.Text.Trim();
        _store.Add(new Expense(Guid.NewGuid(), _date.Value.Date, category, _amount.Value, _note.Text.Trim()));

        _amount.Value = 0;
        _note.Clear();
        UpdateCategories();
        _category.Text = category;
        if (!_all.Checked) _month.Value = _date.Value;   // чтобы новая запись не оказалась скрыта фильтром
        RefreshView();
        _amount.Focus();
        _amount.Select(0, _amount.Text.Length);
    }

    private void DeleteSelected()
    {
        if (_grid.CurrentRow?.DataBoundItem is not RowView row) return;
        var answer = MessageBox.Show(this, $"Удалить запись «{row.Category}, {row.Amount} {Currency}»?",
            "Удаление", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
        if (answer != DialogResult.Yes) return;
        _store.Remove(row.Id);
        RefreshView();
    }

    private void Export()
    {
        using var dialog = new SaveFileDialog
        {
            Filter = "CSV (*.csv)|*.csv",
            FileName = $"expenses-{DateTime.Now:yyyy-MM-dd}.csv"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            ExpenseStore.ExportCsv(dialog.FileName, Filtered().OrderBy(e => e.Date));
            MessageBox.Show(this, "Файл сохранён.", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (IOException ex)
        {
            MessageBox.Show(this, "Не удалось сохранить файл: " + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
