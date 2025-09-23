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
        private CancellationTokenSource? _danceCts; // отмена "Танца"
        private CancellationTokenSource? _snowCts;  // отмена "Снега"
        private readonly Random _rnd = new();

        private int SpeedMs => (int)SpeedStepper.Value;

        public Snowman()
        {
            InitializeComponent();

            // Применим начальные значения контролов
            SnowmanContainer.Opacity = OpacitySlider.Value;
            SpeedValueLabel.Text = $"{SpeedMs} мс";

            // Как только слой снега получит размер — запустим снег
            SnowOverlay.SizeChanged += (s, e) =>
            {
                if (SnowOverlay.Width > 0 && SnowOverlay.Height > 0 && _snowCts == null)
                {
                    _snowCts = new CancellationTokenSource();
                    _ = StartSnowAsync(_snowCts.Token);
                }
            };
        }

        /// <summary>Изменение прозрачности — просто меняем Opacity у контейнера снеговика.</summary>
        private void OpacitySlider_ValueChanged(object sender, ValueChangedEventArgs e)
        {
            SnowmanContainer.Opacity = e.NewValue;
        }

        /// <summary>Обновляем подпись со скоростью.</summary>
        private void SpeedStepper_ValueChanged(object sender, ValueChangedEventArgs e)
        {
            SpeedValueLabel.Text = $"{(int)e.NewValue} мс";
        }

        /// <summary>
        /// Выполнение выбранного действия из Picker.
        /// Перед новым действием всегда останавливаем текущий "Танец".
        /// </summary>
        private async void DoButton_Clicked(object sender, EventArgs e)
        {
            var choice = ActionPicker.SelectedItem as string;
            if (string.IsNullOrWhiteSpace(choice))
            {
                await DisplayAlert("Действие", "Выберите действие из списка.", "OK");
                return;
            }

            // Остановить текущий танец (если был)
            _danceCts?.Cancel();
            _danceCts = null;

            StatusLabel.Text = choice;

            switch (choice)
            {
                case "Спрятать":
                    SnowmanContainer.IsVisible = false;
                    break;

                case "Показать":
                    SnowmanContainer.IsVisible = true;
                    SnowmanContainer.Opacity = OpacitySlider.Value;
                    await SnowmanContainer.ScaleTo(1.0, 1);
                    await SnowmanContainer.TranslateTo(0, 0, 1);
                    break;

                case "Сменить цвет":
                    if (await DisplayAlert("Сменить цвет", "Изменить случайно цвета шарфа и ведра?", "Да", "Нет"))
                    {
                        var scarf = Color.FromRgb(_rnd.Next(120, 256), _rnd.Next(30, 160), _rnd.Next(30, 160));
                        var bucket = Color.FromRgb(_rnd.Next(90, 170), _rnd.Next(60, 120), _rnd.Next(20, 60));
                        Scarf.BackgroundColor = scarf;
                        BucketBody.BackgroundColor = bucket;
                        BucketBrim.BackgroundColor = Darker(bucket, 0.85);
                    }
                    break;

                case "Растопить":
                    SnowmanContainer.IsVisible = true;
                    // Бонус для атмосферы: слегка «роняем» ведро перед растапливанием
                    await Task.WhenAll(
                        SnowmanContainer.FadeTo(0.0, (uint)SpeedMs),
                        SnowmanContainer.ScaleTo(0.4, (uint)SpeedMs, Easing.CubicIn)
                    );
                    break;

                case "Танцевать":
                    SnowmanContainer.IsVisible = true;
                    _danceCts = new CancellationTokenSource();
                    _ = DanceLoopAsync(_danceCts.Token);
                    break;
            }
        }

        /// <summary>
        /// Бесконечная анимация «танец»: движение влево-вправо, затем в центр.
        /// Амплитуда ограничена, чтобы снеговик не «уплывал» под панель.
        /// </summary>
        private async Task DanceLoopAsync(CancellationToken ct)
        {
            const double dx = 30; // пикселей по X
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    await SnowmanContainer.TranslateTo(-dx, 0, (uint)SpeedMs, Easing.SinInOut);
                    if (ct.IsCancellationRequested) break;

                    await SnowmanContainer.TranslateTo(+dx, 0, (uint)SpeedMs, Easing.SinInOut);
                    if (ct.IsCancellationRequested) break;

                    await SnowmanContainer.TranslateTo(0, 0, (uint)SpeedMs, Easing.SinInOut);
                }
            }
            catch (TaskCanceledException) { }
            finally
            {
                await SnowmanContainer.TranslateTo(0, 0, 1);
            }
        }

        /// <summary>
        /// Запускает фоновую «метель»: создаёт снежинки и для каждой крутит цикл падения.
        /// Снежинка — маленький BoxView с большим скруглением (получается кружок).
        /// </summary>
        private async Task StartSnowAsync(CancellationToken ct)
        {
            // Кол-во снежинок подбираем умеренным: красиво и не грузит устройство
            const int flakesCount = 36;
            var flakes = new List<BoxView>(flakesCount);

            // Локальная функция — добавляет снежинку и запускает её анимацию
            async Task SpawnAndFallAsync()
            {
                // пока не отменили — создаём новую снежинку, как только предыдущая упала
                while (!ct.IsCancellationRequested)
                {
                    // Размер и прозрачность — случайные, для «живости»
                    int size = _rnd.Next(4, 9);
                    double opacity = 0.55 + _rnd.NextDouble() * 0.45;

                    var flake = new BoxView
                    {
                        Color = Colors.White,
                        CornerRadius = size / 2f,
                        WidthRequest = size,
                        HeightRequest = size,
                        Opacity = opacity,
                        InputTransparent = true
                    };

                    // Позиционируем пропорционально по X, а размер — в пикселях
                    double x = _rnd.NextDouble();       // 0..1
                    double y = -0.06 - _rnd.NextDouble() * 0.15; // чуть выше экрана
                    AbsoluteLayout.SetLayoutFlags(flake, AbsoluteLayoutFlags.PositionProportional);
                    AbsoluteLayout.SetLayoutBounds(flake, new Rect(x, y, size, size));

                    SnowOverlay.Add(flake);

                    // Параметры падения
                    double drift = _rnd.Next(-25, 26);  // лёгкий снос по X
                    int fallMs = _rnd.Next(3500, 8000); // длительность падения

                    try
                    {
                        // Ждём кадр разметки, чтобы гарантированно знали высоту слоя
                        await Task.Yield();
                        // Падение — просто перенос по Y до низа плюс небольшой запас
                        await flake.TranslateTo(drift, SnowOverlay.Height + 20, (uint)fallMs, Easing.Linear);
                    }
                    catch (TaskCanceledException) { /* отменили снег — нормально */ }
                    finally
                    {
                        SnowOverlay.Remove(flake); // убираем упавшую снежинку
                    }

                    // Небольшая пауза перед новым «рождением» снежинки
                    await Task.Delay(_rnd.Next(150, 600), ct).ConfigureAwait(false);
                }
            }

            // Запускаем несколько «потоков снегопада» — так равномернее
            var tasks = new List<Task>();
            for (int i = 0; i < flakesCount; i++)
                tasks.Add(SpawnAndFallAsync());

            try { await Task.WhenAll(tasks); }
            catch (TaskCanceledException) { /* нормальная остановка */ }
        }

        /// <summary>
        /// Хелпер для затемнения цвета (для ободка ведра).
        /// factor 0.0..1.0 — чем меньше, тем темнее.
        /// </summary>
        private static Color Darker(Color baseColor, double factor)
        {
            factor = Math.Clamp(factor, 0.0, 1.0);
            return Color.FromRgb(
                (int)(baseColor.Red * 255 * factor),
                (int)(baseColor.Green * 255 * factor),
                (int)(baseColor.Blue * 255 * factor)
            );
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            // Чистим бесконечные анимации при уходе со страницы
            _danceCts?.Cancel();
            _snowCts?.Cancel();
        }
    }
}
