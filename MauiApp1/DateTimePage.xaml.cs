using Microsoft.Maui.Layouts;

namespace MauiApp1;

public partial class DateTimePage : ContentPage
{
	Label mis_on_valitud;
	DatePicker datePicker;
	TimePicker timePicker;
	Picker picker;
	Slider slider;
	Stepper stepper;
    AbsoluteLayout al;
	public DateTimePage()
	{
		//InitializeComponent();
		mis_on_valitud = new Label
		{
			Text = "Siin kuvatakse valitud kuupaev voi kellaeg",
			FontSize = 20,
			TextColor = Colors.Blue,
			FontFamily = "Verdana",
			HorizontalTextAlignment = TextAlignment.Start
		};
		datePicker = new DatePicker
		{
			FontSize = 20,
			Background = Color.FromRgb(200, 200, 100),
			TextColor = Colors.Black,
			FontFamily = "Verdana",
			MinimumDate = DateTime.Now.AddDays(-7),
			MaximumDate = new DateTime(2100, 12, 31),
			Date = DateTime.Now,
			Format = "D"

		};
		timePicker = new TimePicker
		{
			FontSize = 20,
			Background = Color.FromRgb(200, 200, 100),
			TextColor = Colors.Black,
			FontFamily = "Verdana",
			Time = new TimeSpan(12, 0, 0),
			Format = "T"
		};
		timePicker.PropertyChanged += (s, e) =>
		{
			if (e.PropertyName == TimePicker.TimeProperty.PropertyName)
			{
				mis_on_valitud.Text = $"valisite kellaaaja: {timePicker.Time}";
			}
		};
		picker = new Picker
		{
			Title = "Vali uks",
			FontSize = 20,
			Background = Color.FromRgb(200, 200, 100),
			TextColor = Colors.Black,
			FontFamily = "Verdana",
			ItemsSource = new List<string> { "üks", "kaks", "kolm", "neli", "viis" }

		};
		//picker.Items.Add("Kuus");
		picker.SelectedIndexChanged += (s, e) =>
		{
			if (picker.SelectedIndex != -1)
			{
				mis_on_valitud.Text = $"Valisid: {picker.Items[picker.SelectedIndex]}";
			}
		};


		datePicker.DateSelected += Kuupaeva_valimine;

		slider = new Slider
		{
			Minimum = 0,
			Maximum = 100,
			Value = 50,
			Background = Color.FromRgb(200, 200, 100),
			ThumbColor = Colors.Red,
			MinimumTrackColor = Colors.Green,
			MaximumTrackColor = Colors.Blue
		};

		slider.ValueChanged += (s, e) =>
		{
			mis_on_valitud.FontSize = e.NewValue;
			mis_on_valitud.Rotation = e.NewValue;
		};
		stepper = new Stepper
		{
			Minimum = 10,
			Maximum = 100,
			Increment = 1,
			Value = 20,
			Background = Color.FromRgb(200, 200, 100),
			HorizontalOptions = LayoutOptions.Center
		};
		stepper.ValueChanged += (s, e) =>
		{
			mis_on_valitud.Text = $"Steper value: {e.NewValue}";
		};


		al = new AbsoluteLayout { Children = { mis_on_valitud, datePicker, timePicker, picker, slider, stepper } };
		//AbsoluteLayout.SetLayoutBounds(mis_on_valitud, new Rect(0.5, 0.0, 0.9, 0.2));
		//AbsoluteLayout.SetLayoutFlags(mis_on_valitud, AbsoluteLayoutFlags.All);
		//AbsoluteLayout.SetLayoutBounds(mis_on_valitud, new Rect(0.5, 0.2, AbsoluteLayout.AutoSize, 0.2));
		//AbsoluteLayout.SetLayoutFlags(mis_on_valitud, AbsoluteLayoutFlags.All);
		//AbsoluteLayout.SetLayoutBounds(datePicker, new Rect(0.5, 0.4, 0.9, 0.2));
		//AbsoluteLayout.SetLayoutFlags(datePicker, AbsoluteLayoutFlags.All);
		//AbsoluteLayout.SetLayoutBounds(timePicker, new Rect(0.5, 0.6, 0.9, 0.2));
		//AbsoluteLayout.SetLayoutFlags(timePicker, AbsoluteLayoutFlags.All);
		//AbsoluteLayout.SetLayoutBounds(picker, new Rect(0.5, 0.6, 0.9, 0.2));
		//AbsoluteLayout.SetLayoutFlags(picker, AbsoluteLayoutFlags.All);
		//AbsoluteLayout.SetLayoutBounds(slider, new Rect(0.5, 0.8, 0.9, 0.2));
		//AbsoluteLayout.SetLayoutFlags(slider, AbsoluteLayoutFlags.All);
		//AbsoluteLayout.SetLayoutBounds(stepper, new Rect(0.5, 1.0, 0.9, 0.2));
		//AbsoluteLayout.SetLayoutFlags(stepper, AbsoluteLayoutFlags.All);


		View[] elementid = new View[]
			{
			mis_on_valitud, datePicker, timePicker, picker, slider, stepper
		};
		for (int i = 0; i < elementid.Length; i++)

		{
			AbsoluteLayout.SetLayoutBounds(elementid[i], new Rect(0.5, 0.1 + i * 0.16, 0.9, 0.15));
			AbsoluteLayout.SetLayoutFlags(elementid[i], AbsoluteLayoutFlags.All);
        }
        Content = al;
    }

    private void Kuupaeva_valimine(object? sender, DateChangedEventArgs e)
    {
        throw new NotImplementedException();
    }
	
}