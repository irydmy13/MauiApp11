namespace MauiApp1;

public partial class TextPage : ContentPage
{
    Label lblTekst;
    Editor editorTekst;
    VerticalStackLayout hsl; // <-- было HorizontalStackLayout
    Button btn;

    public TextPage()
    {
        lblTekst = new Label
        {
            Text = "Tekst: ",
            FontSize = 25,
            TextColor = Colors.Black,
            BackgroundColor = Colors.LightGray,
            FontFamily = "verdana",
            HorizontalTextAlignment = TextAlignment.Center,
            Margin = new Thickness(10)
        };
        editorTekst = new Editor
        {
            FontSize = 20,
            BackgroundColor = Color.FromRgb(200, 200, 100),
            TextColor = Colors.Black,
            FontFamily = "verdana",
            AutoSize = EditorAutoSizeOption.TextChanges,
            Placeholder = "Sisesta tekst",
            FontAttributes = FontAttributes.Italic,
        };
        editorTekst.TextChanged += EditorTekst_TextChanged;

        btn = new Button
        {
            Text = "Loe tekst"
        };
        btn.Clicked += Btn_Clicked;

        hsl = new VerticalStackLayout // тип инициализируемого объекта совпадает с объ€влением
        {
            BackgroundColor = Color.FromRgb(120, 30, 50),
            Children = { lblTekst, editorTekst, btn },
            HorizontalOptions = LayoutOptions.Center
        };

        Content = hsl;
    }

    private async void Btn_Clicked(object? sender, EventArgs e)
    {
        IEnumerable<Locale> locales = await TextToSpeech.Default.GetLocalesAsync();

        SpeechOptions options = new SpeechOptions
        {
            Pitch = 1.5f,   // 0.0 - 2.0
            Volume = 0.75f, // 0.0 - 1.0
            Locale = locales.FirstOrDefault(l =>l.Language == "et-EE")
        };

        var text = editorTekst.Text;
        if (string.IsNullOrWhiteSpace(text))
        {
            await DisplayAlert("Viga", "Palun sisesta tekst", "OK");
            return;
        }

        try
        {
            await TextToSpeech.SpeakAsync(text, options);
        }
        catch (Exception ex)
        {
            await DisplayAlert("TTS viga", ex.Message, "OK");
        }
    }

    private void EditorTekst_TextChanged(object? sender, TextChangedEventArgs e)
    {
        lblTekst.Text = editorTekst.Text;
    }
}
