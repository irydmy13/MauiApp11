namespace MauiApp1;

public partial class FigurePage : ContentPage
{
	BoxView boxView;
	Random random = new Random();
	HorizontalStackLayout hsl;
	public FigurePage()
	{
		int red = random.Next(0, 256);
		int green = random.Next(0, 256);
		int blue = random.Next(0, 256);
		boxView = new BoxView
		{
			Color = Color.FromRgb(red, green, blue),
			WidthRequest = DeviceDisplay.MainDisplayInfo.Width / 3,
			HeightRequest = DeviceDisplay.MainDisplayInfo.Height / 3,
			CornerRadius = 20,
			HorizontalOptions = LayoutOptions.Center,
			VerticalOptions = LayoutOptions.Center,
			BackgroundColor = Color.FromRgba(0,0,0,0)
		};
		TapGestureRecognizer tapGesture = new TapGestureRecognizer();
		tapGesture.Tapped += klik_boksi_peal;
		boxView.GestureRecognizers.Add(tapGesture);
		hsl = new HorizontalStackLayout
		{
			Children = { boxView },
			HorizontalOptions = LayoutOptions.Center,
			VerticalOptions = LayoutOptions.Center,
		};
		Content = hsl;
	}
    private void klik_boksi_peal(object? sender, TappedEventArgs e)
    {
        boxView.Color = Color.FromRgb(random.Next(0, 256), random.Next(0, 256), random.Next(0, 256));
		boxView.WidthRequest = random.Next(50, (int)(DeviceDisplay.MainDisplayInfo.Width / 2));
		boxView.HeightRequest = random.Next(50, (int)(DeviceDisplay.MainDisplayInfo.Height / 2));
		boxView.CornerRadius = random.Next(0, 101);
    }
}