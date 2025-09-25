using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Layouts;

namespace MauiApp1
{
    /// <summary>
    /// Логика страницы Snowman.
    /// Что здесь есть:
    ///  • Управление действиями (Спрятать/Показать/Сменить цвет/Растопить/Танцевать)
    ///  • «Падающий снег» — независимая бесконечная анимация маленьких кружков.
    ///  • Улучшенное ведро: корпус + поля + ручка (цвет можно менять в «Сменить цвет»).
    ///
    /// Принципы:
    ///  • Все манипуляции делаем над контейнером SnowmanContainer — так проще анимировать всё сразу.
    ///  • Снежинки добавляем во внешний слой SnowOverlay и анимируем TranslateTo вниз.
    ///  • Для отмены бесконечных анимаций держим CancellationTokenSource.
    /// </summary>
    public partial class Snowman : ContentPage
    {
        Random random = new Random();
        AbsoluteLayout taust;
        Grid tahvel;

        public Snowman()
        {
            taust = new AbsoluteLayout
            {
                BackgroundColor = Color.FromRgb(10, 100, 100),

            };
            var tausta_pilt = new Image
            {
                Source = "snowtaust.png",
                Aspect = Aspect.AspectFill
            };
            AbsoluteLayout.SetLayoutBounds(tausta_pilt, new Rect(0, 0, 1, 1));
            AbsoluteLayout.SetLayoutFlags(tausta_pilt, AbsoluteLayoutFlags.All);


            taust.Children.Add(tausta_pilt);
            tahvel = new Grid
            {
                BackgroundColor = Colors.Transparent,
                HeightRequest = (int)DeviceDisplay.MainDisplayInfo.Height,
                WidthRequest = (int)DeviceDisplay.MainDisplayInfo.Width
            };

            // Tap (topeltklõps)
            var tapGesture = new TapGestureRecognizer { NumberOfTapsRequired = 2 };
            tapGesture.Tapped += TapGesture_Tapped;
            tahvel.GestureRecognizers.Add(tapGesture);

            // Pan (lohista ja lase lahti)
            var panGesture = new PanGestureRecognizer();
            panGesture.PanUpdated += PanGesture_PanUpdated;
            tahvel.GestureRecognizers.Add(panGesture);

            taust.Children.Add(tahvel);
            Content = taust;
        }

        private void PanGesture_PanUpdated(object? sender, PanUpdatedEventArgs e)
        {
            if (e.StatusType == GestureStatus.Completed)
            {
                LisaLumi(e.TotalX + random.Next(0, (int)DeviceDisplay.MainDisplayInfo.Width / 4), e.TotalY, "snow.png");
            }
        }

        private void TapGesture_Tapped(object? sender, TappedEventArgs e)
        {
            var point = e.GetPosition(tahvel);
            if (point == null)
                return;

            LisaLumi(point.Value.X, point.Value.Y, "snowdrops.png");
        }

        private async void LisaLumi(double x, double y, string file)
        {
            var lumi = new Image
            {
                Source = file,
                HeightRequest = random.Next(20, 200),
                WidthRequest = random.Next(20, 200),
            };

            // Asetame algselt punktile x,y
            AbsoluteLayout.SetLayoutBounds(lumi, new Rect(x, y, lumi.WidthRequest, lumi.HeightRequest));
            AbsoluteLayout.SetLayoutFlags(lumi, AbsoluteLayoutFlags.None);

            taust.Children.Add(lumi);

            // Anname juhusliku "tuule" (x nihke) ja kiiruse
            double targetX = x + random.Next(-50, 50);  // veidi külgsuunas
            double targetY = taust.Height;              // ekraani alaossa
            uint duration = (uint)random.Next(3000, 7000); // kestus millisekundites

            // Anima lumesadu (liigub alla ja küljele)
            await lumi.TranslateTo(targetX - x, targetY - y, duration, Easing.Linear);

            // Kui lumi jõuab alla, eemaldame taustast
            taust.Children.Remove(lumi);
        }
    }
}