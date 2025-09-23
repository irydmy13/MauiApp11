using Microsoft.Maui.Controls;

namespace MauiApp1;

public partial class StartPage : ContentPage
{

    public List<ContentPage> lehed = new List<ContentPage>()
    {
        new TextPage(),
        new FigurePage(),
        new TimerPage(),
        new DateTimePage(),

        new Snowman(),
        new GridPage(),

        new Valgusfoor(),
        new Snowman(),
        new GrigPage()
    };

    public List<string> tekstid = new List<string>()

    { "Tee lahti leht Tekst'ga", "Tee lahti Figure leht","KГ¤ivita taimeri", "KuupГ¤evad ja kellaajad", "Valgusfoor", "Lumememm", "Grid" };


    ScrollView sv;             
    VerticalStackLayout vsl;   

    public StartPage()
    {
        //InitializeComponent();
        Title = "Avaleht";

      
        BackgroundImageSource = "fon.jpg";

        vsl = new VerticalStackLayout
        {
            Spacing = 15,
            Padding = 20
        };

  
        for (int i = 0; i < lehed.Count; i++)
        {
            Button nupp = new Button
            {
                Background = Colors.Black,
                Text = tekstid[i],

                BackgroundColor = Color.FromArgb("#2196F3"),
                FontSize = 20,

                TextColor = Colors.White,
                CornerRadius = 20,
                FontFamily = "nautilus",
                ZIndex = i 
            };

            vsl.Add(nupp);

            nupp.Clicked += Nupp_Clicked;
        }

        sv = new ScrollView { Content = vsl };

        Content = sv;
    }

    private async void Nupp_Clicked(object? sender, EventArgs e)
    {
        Button nupp = (Button)sender;

        await Navigation.PushAsync(lehed[nupp.ZIndex]);
    }
}
