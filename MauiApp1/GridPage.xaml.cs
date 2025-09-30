using System.Linq;
using Microsoft.Maui.Controls;

#if ANDROID
using Android.Media;
#endif
#if WINDOWS
using System.Media;
#endif

namespace MauiApp1;

public partial class GridPage : ContentPage
{
    Grid grid;
    BoxView box;

    int size = 3;
    string current = "X";
    bool vsBot = false;
    readonly Random rng = new();

    // клетки и метки
    ContentView[,] cells;
    Label[,] marks;

    // цвета линий/клеток/текста
    readonly Color cellColor = Color.FromArgb("#F7F3EE");
    Color lineColor = Color.FromArgb("#3A3F45");
    Color xColor = Colors.Black, oColor = Colors.Black;

    // переключатель фона страницы
    bool darkBg = false;

    // состояние игры
    bool gameOver = false;

    // цвета для кнопок
    readonly Color btnBgLight = Color.FromArgb("#D1C1F2");
    readonly Color btnTextLight = Color.FromArgb("#30343A");
    readonly Color btnBgDark = Color.FromArgb("#DAA520");
    readonly Color btnTextDark = Colors.Black;

    // значки игроков (перекрашиваются через TextColor)
    string symbolX = "X";
    string symbolO = "O";
    string SymOf(string who) => who == "X" ? symbolX : symbolO;

    // уровень бота
    enum BotLevel { Off, Easy, Medium, Hard }
    BotLevel botLevel = BotLevel.Off;

    // по умолчанию бот  за O
    string botSide = "O";

    // звук
    byte[]? _wavBytes;

    public GridPage()
    {
        InitializeComponent();
        grid = BoardGrid;
        box = new BoxView();

        ApplyThemeColors();  // задаём цвет линии под текущую тему
        BuildBoard();        // строим поле
        UpdateGridTheme();   // применяем цвет линий
        UpdateButtonsTheme();
        UpdateFrameTheme();
    }

    // Цвета темы (линии и сетки)
    void ApplyThemeColors()
    {
        // клетки
        lineColor = darkBg ? Color.FromArgb("#DAA520") : Color.FromArgb("#3A3F45");
    }

    // Применить тему к сетке (линии)
    void UpdateGridTheme()
    {
        if (BoardGrid != null)
        {
            BoardGrid.BackgroundColor = lineColor;
        }
    }

    // Рамка вокруг поля
    void UpdateFrameTheme()
    {
        var border = this.FindByName<Border>("OuterBorder");
        if (border == null) return;

        border.Stroke = darkBg ? Colors.Black : Colors.White;
    }

    // СЕТКА
    void BuildBoard()
    {
        BoardGrid.Children.Clear();
        BoardGrid.RowDefinitions.Clear();
        BoardGrid.ColumnDefinitions.Clear();

        // линии через spacing
        const int t = 6;
        BoardGrid.BackgroundColor = lineColor;
        BoardGrid.Padding = t;
        BoardGrid.RowSpacing = t;
        BoardGrid.ColumnSpacing = t;

        for (int i = 0; i < size; i++)
        {
            BoardGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
            BoardGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        }

        cells = new ContentView[size, size];
        marks = new Label[size, size];

        for (int r = 0; r < size; r++)
            for (int c = 0; c < size; c++)
            {
                var lbl = new Label
                {
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    FontSize = size <= 3 ? 48 : 36,
                    TextColor = xColor,
                    Text = ""
                };

                var cv = new ContentView
                {
                    BackgroundColor = cellColor,
                    Content = lbl
                };

                // обработчик тапа
                var tap = new TapGestureRecognizer();
                int rr = r, cc = c;
                tap.Tapped += (s, e) => OnCellTapped(rr, cc);
                cv.GestureRecognizers.Add(tap);

                Grid.SetRow(cv, r);
                Grid.SetColumn(cv, c);
                BoardGrid.Children.Add(cv);

                cells[r, c] = cv;
                marks[r, c] = lbl;
            }

        // новое поле — активно
        gameOver = false;
        BoardGrid.InputTransparent = false;
    }

    // проигрывание звука
    async Task EnsureSoundLoadedAsync()
    {
        if (_wavBytes != null) return;
        using var file = await FileSystem.OpenAppPackageFileAsync("gameover.wav");
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        _wavBytes = ms.ToArray();
    }

    async Task PlayGameOverAsync()
    {
#if ANDROID
        try
        {
            var ctx = Android.App.Application.Context;
            using var afd = ctx.Assets.OpenFd("gameover.wav");

            var mp = new MediaPlayer();
            mp.Prepared += (s, e) => mp.Start();
            mp.Completion += (s, e) => { mp.Release(); mp.Dispose(); };
            mp.SetDataSource(afd.FileDescriptor, afd.StartOffset, afd.Length);
            mp.PrepareAsync();
        }
        catch { /* ignore */ }
#elif WINDOWS
        try
        {
            if (_wavBytes == null) await EnsureSoundLoadedAsync();
            using var sp = new SoundPlayer(new MemoryStream(_wavBytes!)); // System.Media
            sp.Load();   // загружаем в память
            sp.Play();   // асинхронно
        }
        catch { /* ignore */ }
#else
        await Task.CompletedTask; // другие платформы — пока тишина
#endif
    }

    // точка завершения: блокируем поле, играем звук и показываем окно
    async Task EndGameAsync(string message)
    {
        gameOver = true;
        BoardGrid.InputTransparent = true; // блокируем все тапы по клеткам
        await PlayGameOverAsync();
        await DisplayAlert("Игра окончена!", message, "OK");
    }

    // ход бота, если сейчас его очередь
    async Task BotMoveIfNeededAsync()
    {
        if (!vsBot || gameOver) return;
        if (current != botSide) return;

        await Task.Delay(200); // чуть-чуть паузы для UX
        var m = ChooseBotMove();   // выбор хода по уровню
        if (m is null) return;
        var (rr, cc) = m.Value;

        // бот ставит свой значок
        marks[rr, cc].Text = SymOf(botSide);
        marks[rr, cc].TextColor = (botSide == "X") ? xColor : oColor;

        if (CheckWinner(out _)) { await EndGameAsync($"{botSide} выиграл!"); return; }
        if (IsDraw()) { await EndGameAsync("Ничья!"); return; }
        SwitchPlayer();
    }

    // ИГРА
    async void OnCellTapped(int r, int c)
    {
        if (gameOver) return; // игра уже завершена
        if (!string.IsNullOrEmpty(marks[r, c].Text)) return;

        marks[r, c].Text = SymOf(current);
        marks[r, c].TextColor = current == "X" ? xColor : oColor;

        if (CheckWinner(out _))
        {
            await EndGameAsync($"{current} выиграл!");
            return;
        }
        if (IsDraw())
        {
            await EndGameAsync("Ничья!");
            return;
        }

        SwitchPlayer();

        // бот ходит за выбранную сторону
        await BotMoveIfNeededAsync();
    }

    void SwitchPlayer() => current = current == "X" ? "O" : "X";

    bool IsDraw()
    {
        foreach (var l in marks)
            if (string.IsNullOrEmpty(l.Text)) return false;
        return !CheckWinner(out _);
    }

    bool CheckWinner(out string w)
    {
        w = "";
        // строки
        for (int r = 0; r < size; r++)
        {
            var s = marks[r, 0].Text;
            if (!string.IsNullOrEmpty(s) && Enumerable.Range(0, size).All(c => marks[r, c].Text == s))
            { w = s; return true; }
        }
        // столбцы
        for (int c = 0; c < size; c++)
        {
            var s = marks[0, c].Text;
            if (!string.IsNullOrEmpty(s) && Enumerable.Range(0, size).All(r => marks[r, c].Text == s))
            { w = s; return true; }
        }
        // диагонали
        var s1 = marks[0, 0].Text;
        if (!string.IsNullOrEmpty(s1) && Enumerable.Range(0, size).All(i => marks[i, i].Text == s1))
        { w = s1; return true; }
        var s2 = marks[0, size - 1].Text;
        if (!string.IsNullOrEmpty(s2) && Enumerable.Range(0, size).All(i => marks[i, size - 1 - i].Text == s2))
        { w = s2; return true; }

        return false;
    }

    (int r, int c)? RandomEmpty()
    {
        var list = new List<(int r, int c)>();
        for (int r = 0; r < size; r++)
            for (int c = 0; c < size; c++)
                if (string.IsNullOrEmpty(marks[r, c].Text))
                    list.Add((r, c));
        return list.Count == 0 ? null : list[rng.Next(list.Count)];
    }

    // пустые клетки
    IEnumerable<(int r, int c)> EmptyCells()
    {
        for (int r = 0; r < size; r++)
            for (int c = 0; c < size; c++)
                if (string.IsNullOrEmpty(marks[r, c].Text))
                    yield return (r, c);
    }

    // проверить: если поставить symbol в (r,c), будет ли победа?
    bool WouldWinAt(int r, int c, string symbol)
    {
        var old = marks[r, c].Text;
        marks[r, c].Text = symbol;
        bool win = CheckWinner(out _);
        marks[r, c].Text = old;
        return win;
    }

    // выбор хода по уровню
    (int r, int c)? ChooseBotMove()
    {
        return botLevel switch
        {
            BotLevel.Easy => RandomEmpty(),
            BotLevel.Medium => ChooseMediumMoveForO(),
            BotLevel.Hard => ChooseSmartMoveForO(),
            _ => null
        };
    }

    // Средний
    (int r, int c)? ChooseMediumMoveForO()
    {
        // выиграть
        foreach (var (r, c) in EmptyCells())
            if (WouldWinAt(r, c, symbolO)) return (r, c);

        // блок
        foreach (var (r, c) in EmptyCells())
            if (WouldWinAt(r, c, symbolX)) return (r, c);

        if (size % 2 == 1)
        {
            int m = size / 2;
            if (string.IsNullOrEmpty(marks[m, m].Text)) return (m, m);
        }
        else
        {
            int a = size / 2 - 1, b = size / 2;
            var centers = new[] { (a, a), (a, b), (b, a), (b, b) };
            foreach (var p in centers)
                if (string.IsNullOrEmpty(marks[p.Item1, p.Item2].Text)) return p;
        }

        // случайно
        return RandomEmpty();
    }

    // Сложный:
    (int r, int c)? ChooseSmartMoveForO()
    {
        // выиграть
        foreach (var (r, c) in EmptyCells())
            if (WouldWinAt(r, c, symbolO)) return (r, c);

        // блок
        foreach (var (r, c) in EmptyCells())
            if (WouldWinAt(r, c, symbolX)) return (r, c);

        // центр(ы)
        if (size % 2 == 1)
        {
            int m = size / 2;
            if (string.IsNullOrEmpty(marks[m, m].Text)) return (m, m);
        }
        else
        {
            int a = size / 2 - 1, b = size / 2;
            var centers = new[] { (a, a), (a, b), (b, a), (b, b) };
            var freeCenters = centers.Where(p => string.IsNullOrEmpty(marks[p.Item1, p.Item2].Text)).ToList();
            if (freeCenters.Count > 0) return freeCenters[rng.Next(freeCenters.Count)];
        }

        // углы
        var corners = new (int r, int c)[] { (0, 0), (0, size - 1), (size - 1, 0), (size - 1, size - 1) };
        var freeCorners = corners.Where(p => p.r >= 0 && p.c >= 0 && p.r < size && p.c < size
                                           && string.IsNullOrEmpty(marks[p.r, p.c].Text)).ToList();
        if (freeCorners.Count > 0) return freeCorners[rng.Next(freeCorners.Count)];

        // стороны
        var sides = new List<(int r, int c)>();
        for (int i = 1; i < size - 1; i++)
        {
            sides.Add((0, i));
            sides.Add((size - 1, i));
            sides.Add((i, 0));
            sides.Add((i, size - 1));
        }
        var freeSides = sides.Where(p => p.r >= 0 && p.c >= 0 && p.r < size && p.c < size
                                      && string.IsNullOrEmpty(marks[p.r, p.c].Text)).ToList();
        if (freeSides.Count > 0) return freeSides[rng.Next(freeSides.Count)];

        // случайно
        return RandomEmpty();
    }

    // "КТО ПЕРВЫЙ?"
    void StartNewGame(string? first = null)
    {
        gameOver = false;
        BoardGrid.InputTransparent = false;

        for (int r = 0; r < size; r++)
            for (int c = 0; c < size; c++)
                marks[r, c].Text = "";

        current = first ?? "X"; 

        // если бот должен ходить первым — даём ему ход сразу
        Device.BeginInvokeOnMainThread(async () =>
        {
            await BotMoveIfNeededAsync();
        });
    }

    // КНОПКИ
    void NewGameClicked(object sender, EventArgs e)
    {
        StartNewGame();        
    }

    async void WhoStartsClicked(object sender, EventArgs e)
    {
        var ch = await DisplayActionSheet("Кто ходит первым?", "Отмена", null, "X", "O", "Случайно");
        if (ch == "Случайно") ch = rng.Next(2) == 0 ? "X" : "O";
        if (ch == "X" || ch == "O")
        {
            StartNewGame(ch);           
        }
    }

    async void ChooseSymbolClicked(object sender, EventArgs e)
    {
        var who = await DisplayActionSheet("Чей цвет?", "Отмена", null, "X", "O");
        if (who is null) return;
        var col = await DisplayActionSheet("Выбери цвет", "Отмена", null, "Черный", "Синий", "Красный", "Зеленый");
        if (col is null) return;
        var pick = col switch { "Синий" => Colors.Blue, "Красный" => Colors.Red, "Зеленый" => Colors.Green, _ => Colors.Black };
        if (who == "X") xColor = pick; else oColor = pick;

        // перекрашиваем уже поставленные
        for (int r = 0; r < size; r++)
            for (int c = 0; c < size; c++)
            {
                if (marks[r, c].Text == symbolX) marks[r, c].TextColor = xColor;
                if (marks[r, c].Text == symbolO) marks[r, c].TextColor = oColor;
            }
    }

    async void ToggleBotClicked(object sender, EventArgs e)
    {
        // выбор уровня бота
        var ch = await DisplayActionSheet("Уровень бота", "Отмена", null, "Выкл", "Лёгкий", "Средний", "Сложный");
        if (string.IsNullOrEmpty(ch) || ch == "Отмена") return;

        botLevel = ch switch
        {
            "Лёгкий" => BotLevel.Easy,
            "Средний" => BotLevel.Medium,
            "Сложный" => BotLevel.Hard,
            _ => BotLevel.Off
        };
        vsBot = botLevel != BotLevel.Off;

        // если включили бота — выбираем, за кого он играет
        if (vsBot)
        {
            var side = await DisplayActionSheet("За кого играет бот?", "Отмена", null, "X", "O");
            if (side == "X" || side == "O")
                botSide = side;
        }

        if (sender is Button btn)
            btn.Text = vsBot ? $"Бот: {ch} ({botSide})" : "Игра без бота";

        // перезапуск партии (учтёт, если бот должен ходить первым)
        StartNewGame();
    }

    async void ChangeSizeClicked(object sender, EventArgs e)
    {
        var ch = await DisplayActionSheet("Размер поля", "Отмена", null, "3×3", "4×4", "5×5");
        int ns = ch switch { "4×4" => 4, "5×5" => 5, _ => 3 };
        if (ns != size)
        {
            size = ns;
            BuildBoard();
            UpdateGridTheme();
            gameOver = false;
            BoardGrid.InputTransparent = false;
        }
    }

    async void ExitClicked(object sender, EventArgs e)
    {
        await Navigation.PopToRootAsync();   // StartPage
    }

    // ПЕРЕКЛЮЧАТЕЛЬ ФОНА СТРАНИЦЫ (чёрный/светлый)
    void ToggleBackgroundClicked(object sender, EventArgs e)
    {
        darkBg = !darkBg;

        // фон страницы
        this.BackgroundColor = darkBg ? Colors.Black : Color.FromArgb("#F7F3EE");

        // подпись на кнопке
        if (sender is Button b)
            b.Text = darkBg ? "Режим: Ночь" : "Режим: День";

        // заголовок
        if (TitleLabel is not null)
            TitleLabel.TextColor = darkBg ? Colors.Goldenrod : Colors.Black;

        ApplyThemeColors();
        UpdateButtonsTheme();
        UpdateGridTheme();
        UpdateFrameTheme();
    }

    void UpdateButtonsTheme()
    {
        var bg = darkBg ? btnBgDark : btnBgLight;
        var fg = darkBg ? btnTextDark : btnTextLight;

        if (ControlsGrid is null) return;
        foreach (var child in ControlsGrid.Children)
            if (child is Button btn)
            {
                btn.BackgroundColor = bg;
                btn.TextColor = fg;
            }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await EnsureSoundLoadedAsync();
    }
}
