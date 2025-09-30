namespace MauiApp1;

public partial class PuslePage : ContentPage
{
    private const int Rows = 3;
    private const int Columns = 4;
    Grid sourcegrid, targetgrid;
    Dictionary<string, Image> pieceImages = new();
    Dictionary<(int row, int col), string> correctPositions = new();
    Image image;
    Image? selectedPiece = null;
    public PuslePage()
    {
        Title = "Pusle leht";
        BackgroundColor = Color.FromRgb(120, 30, 50);
        var mainLayout = new VerticalStackLayout { Spacing = 20, Padding = new Thickness(10) };
        Button newGame = new Button
        {
            Text = "Uus mäng"
        };
        newGame.Clicked += OnNewGameClicked;
        Button pickImage = new Button
        {
            Text = "Vali pilt"
        };
        pickImage.Clicked += async (s, e) => await PickImageAsync();
        image = new Image
        {
            Source = "dotnet_bot.png",
            WidthRequest = 200,
            HeightRequest = 200
        };

        sourcegrid = new Grid { BackgroundColor = Colors.Beige };
        targetgrid = new Grid { BackgroundColor = Colors.LightGray };
        for (int r = 0; r < Rows; r++)
        {
            sourcegrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            targetgrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        }
        for (int c = 0; c < Columns; c++)
        {
            sourcegrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            targetgrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        }


        mainLayout.Children.Add(new HorizontalStackLayout
        {
            Spacing = 40,
            Children = { newGame, pickImage, image }
        });
        mainLayout.Children.Add(sourcegrid);
        mainLayout.Children.Add(targetgrid);

        Content = mainLayout;
    }
    private void InitializePieces()
    {
        pieceImages.Clear();
        correctPositions.Clear();
        sourcegrid.Children.Clear();
        targetgrid.Children.Clear();
        for (int r = 0; r < Rows; r++)
        {
            for (int c = 0; c < Columns; c++)
            {
                string id = $"piece_{r}_{c}";
                var img = new Image
                {
                    Source = $"{id}.png",
                    WidthRequest = 100,
                    HeightRequest = 100,
                };
                pieceImages[id] = img;
                correctPositions[(r, c)] = id;
            }
        }
        for (int r = 0; r < Rows; r++)
        {
            for (int c = 0; c < Columns; c++)
            {
                var target = new Border
                {
                    BackgroundColor = Colors.White,
                    Stroke = Colors.Black,
                    StrokeThickness = 2,
                    Margin = 5,
                    WidthRequest = 100,
                    HeightRequest = 100
                };
            }
        }

    }

    private void AddPiedesGestures(Image img, string id)
    {
        //Drag and drop
        img.GestureRecognizers.Add(new DragGestureRecognizer
        {
            CanDrag = true,
            DragStartingCommand = new Command<DragStartingEventArgs>(args =>
            {
                args.Data.Properties["id"] = id;
            })
        });
        //Tap
        img.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(() =>
            {
                selectedPiece = img;
                img.Opacity = 0.5;
            })
        });
    }
    private void OnPieceTapped(object? sender, TappedEventArgs e)
    {
        // throw new NotImplementedException();
    }
    private async Task PickImageAsync()
    {
        //throw new NotImplementedException();
    }

    private void OnNewGameClicked(object? sender, EventArgs e)
    {
        // throw new NotImplementedException();
    }
}