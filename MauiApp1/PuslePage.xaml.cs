using Microsoft.Maui.Controls;
using SkiaSharp;
using Plugin.Maui.Audio;
using Microsoft.Maui.Graphics;

namespace MauiApp1
{

    public partial class PuslePage : ContentPage
    {
        private const int Rows = 3;
        private const int Cols = 4;

        Grid sourceGrid, targetGrid;
        Dictionary<string, Image> pieceImages = new();
        Dictionary<(int, int), string> correctPositions = new();

        Image? selectedPiece = null;
        Random random = new();
        IAudioManager audioManager = AudioManager.Current;
        Image image;
        public PuslePage()
        {
            Title = "Pusle mäng";

            var mainLayout = new VerticalStackLayout { Spacing = 15 };

            // --- Nupud ---
            var newGameBtn = new Button { Text = "Uus mäng" };
            newGameBtn.Clicked += (s, e) => StartNewGame();

            var pickImageBtn = new Button { Text = "Vali pilt" };
            pickImageBtn.Clicked += async (s, e) => await PickImageAsync();

            image = new Image
            {
                Source = "dotnet_bot.png",
                WidthRequest = 100,
                HeightRequest = 100,
                HorizontalOptions = LayoutOptions.Center
            };
            mainLayout.Children.Add(new HorizontalStackLayout
            {
                Spacing = 20,
                Children = { newGameBtn, pickImageBtn, image }
            });

            // --- Gridi konteiner ---
            var gridsLayout = new Grid { RowSpacing = 10 };
            gridsLayout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
            gridsLayout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });

            // Source grid (tükid segamini)
            sourceGrid = new Grid { BackgroundColor = Colors.Beige };
            // Target grid (lahendamiseks)
            targetGrid = new Grid { BackgroundColor = Colors.LightGray };

            for (int i = 0; i < Rows; i++)
            {
                sourceGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                targetGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            }
            for (int j = 0; j < Cols; j++)
            {
                sourceGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                targetGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            }

            gridsLayout.Add(sourceGrid, 0, 0);
            gridsLayout.Add(targetGrid, 0, 1);

            mainLayout.Children.Add(gridsLayout);

            Content = mainLayout;

            // Alustuseks tükeldame ühe vaikimisi pildi
            InitDefaultPuzzle();
        }

        private void InitDefaultPuzzle()
        {
            // Siin kasutame eelnevalt valmis "piece_X_Y.png" faile
            pieceImages.Clear();
            correctPositions.Clear();


            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Cols; c++)
                {
                    string id = $"piece_{r + 1}_{c + 1}";
                    var img = new Image
                    {
                        Source = $"{id}.png",
                        WidthRequest = 100,
                        HeightRequest = 100
                    };

                    AddPieceGestures(img, id);//

                    pieceImages[id] = img;
                    correctPositions[(r, c)] = id;
                }
            }

            ShufflePieces();
            FillTargetGrid();
        }

        private void ShufflePieces()
        {
            sourceGrid.Children.Clear();

            var shuffled = pieceImages.Values.OrderBy(x => random.Next()).ToList();
            int index = 0;
            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Cols; c++)
                {
                    sourceGrid.Add(shuffled[index], c, r);
                    index++;
                }
            }
        }

        private void FillTargetGrid()
        {
            targetGrid.Children.Clear();

            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Cols; c++)
                {
                    var target = new Border
                    {
                        BackgroundColor = Colors.LightGray,
                        Stroke = Colors.Black,
                        StrokeThickness = 1,
                        WidthRequest = 100,
                        HeightRequest = 100
                    };

                    // Drop
                    target.GestureRecognizers.Add(new DropGestureRecognizer
                    {
                        AllowDrop = true,
                        DropCommand = new Command<DropEventArgs>(args =>
                        {
                            if (args.Data.Properties.TryGetValue("id", out var val) && val is string droppedId)
                            {
                                if (pieceImages.TryGetValue(droppedId, out var img))
                                {
                                    if (img.Parent is Layout oldParent)
                                        oldParent.Children.Remove(img);

                                    target.Content = img;
                                    PlayClick();

                                    //if (CheckWin())
                                    //    ShowWin();
                                }
                            }
                        })
                    });

                    // Tap-to-place
                    target.GestureRecognizers.Add(new TapGestureRecognizer
                    {
                        Command = new Command(() =>
                        {
                            if (selectedPiece != null)
                            {
                                if (selectedPiece.Parent is Layout oldParent)
                                    oldParent.Children.Remove(selectedPiece);

                                target.Content = selectedPiece;
                                selectedPiece = null;
                                PlayClick();

                                //if (CheckWin())
                                //    ShowWin();
                            }
                        })
                    });

                    targetGrid.Add(target, c, r);
                }
            }
        }

        private void AddPieceGestures(Image img, string id)
        {
            // Drag
            img.GestureRecognizers.Add(new DragGestureRecognizer
            {
                CanDrag = true,
                DragStartingCommand = new Command<DragStartingEventArgs>(args =>
                {
                    args.Data.Properties["id"] = id;
                })
            });

            // Tap (valimiseks)
            img.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(() =>
                {
                    selectedPiece = img;
                    img.Opacity = 0.6;
                })
            });
        }

        private bool CheckWin()
        {
            foreach (var cell in targetGrid.Children.OfType<Border>())
            {
                if (cell.Content is not Image img) return false;

                int r = Grid.GetRow(cell);
                int c = Grid.GetColumn(cell);
                string expected = correctPositions[(r, c)];

                if (!img.Source.ToString().Contains(expected))
                    return false;
            }
            return true;
        }

        private async void ShowWin()
        {
            //await Application.Current.MainPage.DisplayAlert("Võit!", "Tubli! Panid pusle kokku!", "OK");
            PlayClick();
        }

        private async void PlayClick()
        {
            var player = audioManager.CreatePlayer(await FileSystem.OpenAppPackageFileAsync("click.mp3"));
            player.Play();
        }

        private void StartNewGame()
        {
            InitDefaultPuzzle();
            ShufflePieces();
            FillTargetGrid();
        }

        // --- Pildi valimine ja jagamine SkiaSharp abil ---
        private async Task PickImageAsync()
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Vali pilt",
                FileTypes = FilePickerFileType.Images
            });
            string filePath;
            if (result == null)
            {
                filePath = "all.png";
                return;
            }
            else
            {
                filePath = result.FullPath;
            }


            image.Source = ImageSource.FromFile(filePath);
            var pieces = SplitImage(filePath, Rows, Cols);
            pieceImages.Clear();
            correctPositions.Clear();

            int index = 0;
            for (int r = 0; r < Rows; r++)
            {
                for (int c = 0; c < Cols; c++)
                {
                    string id = $"piece_{r + 1}_{c + 1}";
                    var img = new Image
                    {
                        Source = pieces[index],
                        WidthRequest = 100,
                        HeightRequest = 100
                    };

                    AddPieceGestures(img, id);
                    pieceImages[id] = img;
                    correctPositions[(r, c)] = id;
                    index++;
                }
            }

            ShufflePieces();
            FillTargetGrid();

        }

        public static List<ImageSource> SplitImage(string filePath, int rows, int cols)
        {
            var result = new List<ImageSource>();

            using var input = File.OpenRead(filePath);
            using var bitmap = SKBitmap.Decode(input);

            int pieceWidth = bitmap.Width / cols;
            int pieceHeight = bitmap.Height / rows;

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    var rect = new SKRectI(c * pieceWidth, r * pieceHeight,
                                           (c + 1) * pieceWidth, (r + 1) * pieceHeight);

                    using var piece = new SKBitmap(rect.Width, rect.Height);
                    using (var canvas = new SKCanvas(piece))
                    {
                        canvas.DrawBitmap(bitmap, rect, new SKRect(0, 0, rect.Width, rect.Height));
                    }

                    using var image = SKImage.FromBitmap(piece);
                    using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                    var bytes = data.ToArray();

                    result.Add(ImageSource.FromStream(() => new MemoryStream(bytes)));
                }
            }

            return result;
        }
    }
}