using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace MauiApp1;

public partial class GridPage : ContentPage
{
    Grid grid;

    public GridPage()
    {
        //InitializeComponent(); // не нужен, всё создаём в коде

        // Фон страницы (чуть серый, чтобы белые клетки было видно)
        BackgroundColor = Colors.LightGray;

        grid = new Grid
        {
            BackgroundColor = Color.FromArgb("#EAEAEA"),
            Padding = 12,          // отступы вокруг сетки
            RowSpacing = 8,        // расстояние между строками
            ColumnSpacing = 8,     // расстояние между столбцами

            RowDefinitions =
            {
                new RowDefinition { Height = GridLength.Star },
                new RowDefinition { Height = GridLength.Star },
                new RowDefinition { Height = GridLength.Star }
            },
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Star }
            }
        };

        // ВАЖНО: показываем сетку на странице
        Content = grid;

        // 3x3 клетки
        for (int rida = 0; rida < 3; rida++)
        {
            for (int veerg = 0; veerg < 3; veerg++)
            {
                var box = new BoxView
                {
                    Color = Colors.White, // сама "клетка"
                    Opacity = 1.0
                    // CornerRadius у BoxView нет — убираем
                };

                var tap = new TapGestureRecognizer();
                tap.Tapped += Tap_Tapped;
                box.GestureRecognizers.Add(tap);

                // добавляем в сетку (столбец, строка)
                grid.Add(box, veerg, rida);
            }
        }
    }

    private async void Tap_Tapped(object sender, TappedEventArgs e)
    {
        var box = (BoxView)sender;
        var r = Grid.GetRow(box);
        var v = Grid.GetColumn(box);
        //await DisplayAlert("Инфо", $"Ряд {r}, Строка {v}", "OK");
    }
}
