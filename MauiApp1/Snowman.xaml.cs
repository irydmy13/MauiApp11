using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;


namespace MauiApp1;

public partial class Snowman : ContentPage
{
    readonly Random _rnd = new();
    uint _speedMs = 800;
    bool _isDancing;

    //Снег
    CancellationTokenSource? _snowCts;
    bool SnowEnabled => SnowSwitch?.IsToggled == true;
    const int SpawnDelayMs = 150; // частота появления снежинок

    public Snowman()
    {
        InitializeComponent();

        SnowmanGroup.AnchorX = 0.5;
        SnowmanGroup.AnchorY = 0.5;

        ActionLabel.Text = "Действие:";
        SpeedSlider.Value = 800;
        SpeedValueLabel.Text = "800 мс";
        OpacitySlider.Value = 1;
        OpacityValueLabel.Text = "100%";
    }

    private const double MinMs = 200;
    private const double MaxMs = 3000;

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (SnowEnabled) StartSnow();
    }

    private void SpeedSlider_ValueChanged(object sender, ValueChangedEventArgs e)
    {
        var duration = MaxMs + MinMs - e.NewValue;
        _speedMs = (uint)duration;
        SpeedValueLabel.Text = $"{(int)_speedMs} мс";
    }

    private async void RunBtn_Clicked(object sender, EventArgs e)
    {
        var action = ActionPicker.SelectedItem as string;
        ActionLabel.Text = $"Действие: {action ?? "(выберите)"}";
        _isDancing = false;

        switch (action)
        {
            case "Скрыть": await HideSnowman(); break;
            case "Показать": await ShowSnowman(); break;
            case "Изменить цвет": await ChangeColor(); break;
            case "Растопить": await Melt(); break;
            case "Танцевать": _ = Dance(); break;
            default:
                await DisplayAlert("Ошибка", "Пожалуйста, выберите действие.", "OK");
                break;
        }
    }

    private async Task HideSnowman()
    {
        await SnowmanGroup.FadeTo(0, _speedMs / 2);
        SnowmanGroup.IsVisible = false;
    }

    private async Task ShowSnowman()
    {
        SnowmanGroup.IsVisible = true;
        await SnowmanGroup.FadeTo(1, _speedMs / 2);
        await SnowmanGroup.TranslateTo(0, 0, 1);
        await SnowmanGroup.ScaleTo(1, 1);
    }

    private Task ChangeColor()
    {
        var snow = Color.FromRgb(_rnd.Next(200, 256),
                                 _rnd.Next(200, 256),
                                 _rnd.Next(200, 256));

        HeadCircle.Fill = new SolidColorBrush(snow);
        MiddleCircle.Fill = new SolidColorBrush(snow);
        BottomCircle.Fill = new SolidColorBrush(snow);

        return Task.CompletedTask;
    }

    private async Task Melt()
    {
        SnowmanGroup.IsVisible = true;
        await SnowmanGroup.ScaleTo(0.6, _speedMs);
        await SnowmanGroup.FadeTo(0, _speedMs);
        SnowmanGroup.IsVisible = false;

        SnowmanGroup.Scale = 1;
        SnowmanGroup.Opacity = OpacitySlider.Value;
    }

    private async Task Dance()
    {
        _isDancing = true;
        SnowmanGroup.IsVisible = true;

        while (_isDancing)
        {
            await SnowmanGroup.TranslateTo(-30, 0, _speedMs, Easing.SinInOut);
            if (!_isDancing) break;
            await SnowmanGroup.TranslateTo(30, 0, _speedMs, Easing.SinInOut);
        }
        await SnowmanGroup.TranslateTo(0, 0, 200);
    }

    private void OpacitySlider_ValueChanged(object sender, ValueChangedEventArgs e)
    {
        SnowmanGroup.Opacity = e.NewValue;
        OpacityValueLabel.Text = $"{(int)(e.NewValue * 100)}%";
    }

    // День/Ночь
    private async void DayNightSwitch_Toggled(object sender, ToggledEventArgs e)
    {
        await this.FadeTo(0.3, 500, Easing.SinInOut);
        this.BackgroundImageSource = e.Value ? "night.jpg" : "day.jpg";
        await this.FadeTo(1, 500, Easing.SinInOut);
    }

    // СНЕГ
    private void SnowSwitch_Toggled(object sender, ToggledEventArgs e) => UpdateSnowState();

    private void UpdateSnowState()
    {
        if (SnowEnabled) StartSnow();
        else StopSnow();
    }

    private void StartSnow()
    {
        if (_snowCts != null && !_snowCts.IsCancellationRequested) return;

        _snowCts = new CancellationTokenSource();
        var ct = _snowCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                while (!ct.IsCancellationRequested && SnowEnabled)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        SpawnSnowflake(ct);
                    });

                    await Task.Delay(SpawnDelayMs, ct);
                }
            }
            catch (TaskCanceledException) { }
        }, ct);
    }

    private void StopSnow()
    {
        if (_snowCts == null) return;
        if (!_snowCts.IsCancellationRequested)
            _snowCts.Cancel();

        _snowCts.Dispose();
        _snowCts = null;
        SnowLayer.Children.Clear();
    }

    private void SpawnSnowflake(CancellationToken ct)
    {
        double layerWidth = SnowLayer.Width > 0 ? SnowLayer.Width : this.Width;
        double layerHeight = SnowLayer.Height > 0 ? SnowLayer.Height : this.Height;
        if (layerWidth <= 0 || layerHeight <= 0) return;

        double size = _rnd.Next(4, 10);
        double startX = _rnd.NextDouble() * (layerWidth - size);
        double startY = -size;

        var flake = new Ellipse
        {
            WidthRequest = size,
            HeightRequest = size,
            Fill = new SolidColorBrush(Colors.White),
            Opacity = _rnd.Next(70, 100) / 100.0,
            InputTransparent = true
        };

        AbsoluteLayout.SetLayoutBounds(flake, new Rect(startX, startY, size, size));
        SnowLayer.Children.Add(flake);

        double endY = layerHeight + size;
        uint fallMs = (uint)_rnd.Next(3000, 7000);
        double drift = _rnd.Next(-30, 31);

        _ = flake.TranslateTo(drift, endY, fallMs, Easing.Linear).ContinueWith(_ =>
        {
            MainThread.BeginInvokeOnMainThread(() => {SnowLayer.Children.Remove(flake);
            });
        });
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        StopSnow();
        _isDancing = false;
    }
}
