using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace MauiApp1;

public partial class Snowman : ContentPage
{
    readonly Random _rnd = new();
    uint _speedMs = 800;
    bool _isDancing;

    public Snowman()
    {
        InitializeComponent();

        SnowmanGroup.AnchorX = 0.5;
        SnowmanGroup.AnchorY = 0.5;

        SpeedLabel.Text = $"Текущая скорость: {(int)_speedMs} мс";
        ActionLabel.Text = "Действие: Показать";
    }

    private const double MinMs = 200;
    private const double MaxMs = 3000;

    public object BackgroundBrush { get; private set; }

    private void SpeedSlider_ValueChanged(object sender, ValueChangedEventArgs e)
    {
        var duration = MaxMs + MinMs - e.NewValue;
        _speedMs = (uint)duration;
        SpeedLabel.Text = $"Скорость: {(int)_speedMs} мс";
    }

    private async void RunBtn_Clicked(object sender, EventArgs e)
    {
        var action = ActionPicker.SelectedItem as string;
        ActionLabel.Text = $"Действие: {action ?? "(выберите)"}";ˇˇ
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
    }

    // День/Ночь 
    private async void DayNightSwitch_Toggled(object sender, ToggledEventArgs e)
    {
        // Плавное затемнение
        await this.FadeTo(0.3, 500, Easing.SinInOut);

        // Смена картинки
        if (e.Value)
        {
            BackgroundBrush.ImageSource = "night.jpg";
        }
        else
        {
            BackgroundBrush.ImageSource = "day.jpg";
        }

        // Плавное возвращение яркости
        await this.FadeTo(1, 500, Easing.SinInOut);
    }
}
