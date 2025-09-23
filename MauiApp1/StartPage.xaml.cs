using Microsoft.Maui.Controls;

namespace MauiApp1;

public partial class StartPage : ContentPage
{
    // список страниц (куда будут переходить кнопки)
    public List<ContentPage> lehed = new List<ContentPage>()
    {
        new TextPage(),
        new FigurePage(),
        new TimerPage(),
        new DateTimePage(),
<<<<<<< HEAD
        new Snowman(),
        new GridPage()
=======
        new Valgusfoor(),
        new Snowman(),
        new GrigPage()
>>>>>>> 23/09
    };

    // тексты для кнопок
    public List<string> tekstid = new List<string>()
<<<<<<< HEAD
    {
        "�������� � �������",
        "������",
        "������",
        "��������",
        "���� � �����",
        "��������",
        "�������� ������"
    };
=======
    { "Tee lahti leht Tekst'ga", "Tee lahti Figure leht","Käivita taimeri", "Kuupäevad ja kellaajad", "Valgusfoor", "Lumememm", "Grid" };
>>>>>>> 23/09

    ScrollView sv;              // скролл для прокрутки
    VerticalStackLayout vsl;    // контейнер для кнопок

    public StartPage()
    {
        //InitializeComponent(); // XAML не использую
        Title = "Avaleht";

        // фон-картинка на всю страницу (положи bg.jpg в папку Resources/Images/)
        BackgroundImageSource = "fon.jpg";

        // создаём вертикальный контейнер
        vsl = new VerticalStackLayout
        {
            Spacing = 15,
            Padding = 20
        };

        // создаём кнопки из списка tekstid
        for (int i = 0; i < lehed.Count; i++)
        {
            Button nupp = new Button
            {
                Background = Colors.Black,
                Text = tekstid[i],
<<<<<<< HEAD
                BackgroundColor = Color.FromArgb("#2196F3"),
                FontSize = 20,
=======
                FontSize = 25,
>>>>>>> 23/09
                TextColor = Colors.White,
                CornerRadius = 20,
                FontFamily = "nautilus",
                ZIndex = i // запомним индекс, чтобы знать, какую страницу открывать
            };

            // добавляем кнопку в контейнер
            vsl.Add(nupp);

            // подписываемся на событие нажатия
            nupp.Clicked += Nupp_Clicked;
        }

        // оборачиваем всё в скролл
        sv = new ScrollView { Content = vsl };

        // делаем скролл основным содержимым страницы
        Content = sv;
    }

    // обработка клика по кнопке
    private async void Nupp_Clicked(object? sender, EventArgs e)
    {
        Button nupp = (Button)sender;
        // открываем страницу по индексу кнопки
        await Navigation.PushAsync(lehed[nupp.ZIndex]);
    }
}
