using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using Plugin.Maui.Audio;

namespace MauiApp1;

public partial class Snowman : ContentPage
{
    readonly Random _rnd = new();
    uint _speedMs = 800;

    // музыка
    private IAudioPlayer? _musicPlayer;

    // снег/танец
    CancellationTokenSource? _danceCts;
    CancellationTokenSource? _snowCts;
    bool SnowEnabled => SnowSwitch?.IsToggled == true;
    const int SpawnDelayMs = 150;

    // границы слайдера скорости
    const double SliderMin = 200;
    const double SliderMax = 3000;

    // эталонные трансформации (из XAML)
    double _gTX, _gTY, _gScale;
    double _hTX, _hTY, _hScale;
    double _mTX, _mTY, _mScale;
    double _bTX, _bTY, _bScale;
    double _bucketTX, _bucketTY, _bucketRot, _bucketOpacity = 1.0;

    public Snowman()
    {
        InitializeComponent();

        SnowmanGroup.AnchorX = 0.5;
        SnowmanGroup.AnchorY = 0.5;

        ActionLabel.Text = "Действие:";

        // стартовые значения скорости (в процентах)
        SpeedSlider.Value = 800;
        _speedMs = (uint)(SliderMax + SliderMin - SpeedSlider.Value);
        var pct = (SpeedSlider.Value - SliderMin) / (SliderMax - SliderMin) * 100.0;
        SpeedValueLabel.Text = $"{Math.Round(pct)}%";

        // стартовая прозрачность (применяем ТОЛЬКО к детям)
        OpacitySlider.Value = 1;
        OpacityValueLabel.Text = "100%";
        SetSnowmanOpacity(OpacitySlider.Value);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // всегда подхватываем эталон из текущего XAML
        Dispatcher.Dispatch(CaptureInitialState);

        if (SnowEnabled) StartSnow();
    }

    private void CaptureInitialState()
    {
        // группа
        _gTX = SnowmanGroup.TranslationX;
        _gTY = SnowmanGroup.TranslationY;
        _gScale = SnowmanGroup.Scale;

        // круги
        _hTX = HeadCircle.TranslationX; _hTY = HeadCircle.TranslationY; _hScale = HeadCircle.Scale;
        _mTX = MiddleCircle.TranslationX; _mTY = MiddleCircle.TranslationY; _mScale = MiddleCircle.Scale;
        _bTX = BottomCircle.TranslationX; _bTY = BottomCircle.TranslationY; _bScale = BottomCircle.Scale;

        // ведро
        if (this.FindByName<Image>("Bucket") is { } bucket)
        {
            _bucketTX = bucket.TranslationX;
            _bucketTY = bucket.TranslationY;
            _bucketRot = bucket.Rotation;
            _bucketOpacity = bucket.Opacity;
        }
    }

    private void ResetSnowmanState()
    {
        // группа
        SnowmanGroup.Scale = _gScale;
        SnowmanGroup.TranslationX = _gTX;
        SnowmanGroup.TranslationY = _gTY;

        // круги
        HeadCircle.TranslationX = _hTX; HeadCircle.TranslationY = _hTY; HeadCircle.Scale = _hScale; HeadCircle.Opacity = 1;
        MiddleCircle.TranslationX = _mTX; MiddleCircle.TranslationY = _mTY; MiddleCircle.Scale = _mScale; MiddleCircle.Opacity = 1;
        BottomCircle.TranslationX = _bTX; BottomCircle.TranslationY = _bTY; BottomCircle.Scale = _bScale; BottomCircle.Opacity = 1;

        // ведро — в исходное
        if (this.FindByName<Image>("Bucket") is { } bucket)
        {
            bucket.Opacity = _bucketOpacity;
            bucket.TranslationX = _bucketTX;
            bucket.TranslationY = _bucketTY;
            bucket.Rotation = _bucketRot;
        }
    }

    // ===== helpers для прозрачности у детей =====
    private void SetSnowmanOpacity(double o)
    {
        foreach (var child in SnowmanGroup.Children)
            if (child is VisualElement ve)
                ve.Opacity = o;
    }

    private Task FadeSnowmanTo(double o, uint ms, Easing? easing = null)
    {
        easing ??= Easing.Linear;
        var tasks = new List<Task>();
        foreach (var child in SnowmanGroup.Children)
            if (child is VisualElement ve)
                tasks.Add(ve.FadeTo(o, ms, easing));
        return Task.WhenAll(tasks);
    }

    // ===== скорость / прозрачность =====
    private void SpeedSlider_ValueChanged(object sender, ValueChangedEventArgs e)
    {
        _speedMs = (uint)(SliderMax + SliderMin - e.NewValue);
        var pct = (e.NewValue - SliderMin) / (SliderMax - SliderMin) * 100.0;
        SpeedValueLabel.Text = $"{Math.Round(pct)}%";
    }

    private void OpacitySlider_ValueChanged(object sender, ValueChangedEventArgs e)
    {
        SetSnowmanOpacity(e.NewValue);
        OpacityValueLabel.Text = $"{(int)(e.NewValue * 100)}%";
    }

    // ===== действия =====
    private async void RunBtn_Clicked(object sender, EventArgs e)
    {
        var action = (ActionPicker.SelectedItem as string)?.Trim() ?? "";

        _danceCts?.Cancel();
        _danceCts = null;

        switch (action)
        {
            case "Скрыть":
                await HideSnowman();
                break;

            case "Показать":
                await ShowSnowman();
                break;

            case "Изменить цвет":
                if (await DisplayAlert("Сменить цвет", "Изменить случайный цвет снеговика?", "Да", "Нет"))
                    await ChangeColor();
                break;

            case "Растопить":
                await MeltSimple();
                break;

            case "Танцевать":
                SnowmanGroup.IsVisible = true;
                _danceCts = new CancellationTokenSource();
                _ = DanceLoopAsync(_danceCts.Token);
                break;

            default:
                await DisplayAlert("Ошибка", "Выберите действие в списке.", "OK");
                break;
        }
    }

    private async Task HideSnowman()
    {
        await FadeSnowmanTo(0, _speedMs / 2, Easing.Linear);
        SnowmanGroup.IsVisible = false;
    }

    private async Task ShowSnowman()
    {
        SnowmanGroup.IsVisible = true;
        ResetSnowmanState();

        await SnowmanGroup.TranslateTo(_gTX, _gTY, 1);
        await SnowmanGroup.ScaleTo(_gScale, 1);
        await FadeSnowmanTo(OpacitySlider.Value, _speedMs / 2, Easing.Linear);
    }

    private Task ChangeColor()
    {
        var snow = Color.FromRgb(
            _rnd.Next(200, 256),
            _rnd.Next(200, 256),
            _rnd.Next(200, 256));

        HeadCircle.Fill = new SolidColorBrush(snow);
        MiddleCircle.Fill = new SolidColorBrush(snow);
        BottomCircle.Fill = new SolidColorBrush(snow);
        return Task.CompletedTask;
    }

    // простая «Оттепель»
    private async Task MeltSimple()
    {
        SnowmanGroup.IsVisible = true;

        await Task.WhenAll(
            FadeSnowmanTo(0.0, _speedMs, Easing.Linear),
            SnowmanGroup.ScaleTo(0.4, _speedMs, Easing.CubicIn)
        );

        ResetSnowmanState();
        SetSnowmanOpacity(0); // а не Opacity у контейнера
    }

    private async Task DanceLoopAsync(CancellationToken ct)
    {
        const double dx = 30;
        try
        {
            while (!ct.IsCancellationRequested)
            {
                await SnowmanGroup.TranslateTo(_gTX - dx, _gTY, _speedMs, Easing.SinInOut);
                if (ct.IsCancellationRequested) break;

                await SnowmanGroup.TranslateTo(_gTX + dx, _gTY, _speedMs, Easing.SinInOut);
                if (ct.IsCancellationRequested) break;

                await SnowmanGroup.TranslateTo(_gTX, _gTY, _speedMs, Easing.SinInOut);
            }
        }
        catch (TaskCanceledException) { }
        finally
        {
            await SnowmanGroup.TranslateTo(_gTX, _gTY, 1);
        }
    }

    // ===== день/ночь =====
    private async void DayNightSwitch_Toggled(object sender, ToggledEventArgs e)
    {
        await this.FadeTo(0.3, 500, Easing.SinInOut);
        this.BackgroundImageSource = e.Value ? "night.jpg" : "day.jpg";
        Resources["PrimaryTextColor"] = e.Value ? Colors.White : Colors.Black;
        await this.FadeTo(1, 500, Easing.SinInOut);
    }

    // ===== снег =====
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
                    await MainThread.InvokeOnMainThreadAsync(() => SpawnSnowflake(ct));
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
            Opacity = _rnd.Next(55, 100) / 100.0,
            InputTransparent = true
        };

        AbsoluteLayout.SetLayoutBounds(flake, new Rect(startX, startY, size, size));
        SnowLayer.Children.Add(flake);

        double endY = layerHeight + size;
        uint fallMs = (uint)_rnd.Next(3500, 8000);
        double drift = _rnd.Next(-25, 26);

        _ = flake.TranslateTo(drift, endY, fallMs, Easing.Linear).ContinueWith(_ =>
        {
            MainThread.BeginInvokeOnMainThread(() => SnowLayer.Children.Remove(flake));
        });
    }

    // ===== музыка =====
    private async void MusicButton_Clicked(object sender, EventArgs e)
    {
        try
        {
            if (_musicPlayer == null)
            {
                var stream = await FileSystem.OpenAppPackageFileAsync("christmas.wav");
                _musicPlayer = AudioManager.Current.CreatePlayer(stream);
                _musicPlayer.Loop = true;
            }

            if (_musicPlayer.IsPlaying)
            {
                _musicPlayer.Pause();
                MusicButton.Text = "▶️ Музыка";
            }
            else
            {
                _musicPlayer.Play();
                MusicButton.Text = "⏸️ Пауза";
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Аудио", $"Не удалось воспроизвести файл: {ex.Message}", "OK");
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        _danceCts?.Cancel();
        StopSnow();

        // стоп музыка
        try
        {
            if (_musicPlayer != null)
            {
                _musicPlayer.Stop();
                _musicPlayer.Dispose();
                _musicPlayer = null;
            }
        }
        catch { /* ignore */ }
    }
}
