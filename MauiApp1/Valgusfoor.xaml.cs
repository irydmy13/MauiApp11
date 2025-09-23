using Microsoft.Maui.Controls;                     // Подключаем пространство имён с визуальными компонентами MAUI

namespace MauiApp1;                                // Пространство имён приложения

public partial class Valgusfoor : ContentPage      // Страница (экран) MAUI; partial — часть класса дополняется XAML
{                                                  // Начало объявления класса

    // включён ли светофор
    bool isOn = false;                             // Флаг: работает ли логика светофора

    // выбран ли ночной режим
    bool isNight = false;                          // Флаг: активирован ли ночной (мигающий) режим

    // таймеры
    IDispatcherTimer? dayTimer;                    // Таймер для дневного цикла (смена фаз)
    IDispatcherTimer? nightTimer;                  // Таймер для ночного мигания жёлтого
    IDispatcherTimer? clockTimer;                  // Таймер для часов в заголовке/лейбле

    // фазы днём
    enum Phase { Red, Yellow, Green }              // enum — перечисление возможных фаз светофора днём
    Phase current = Phase.Red;                     // Текущая фаза по умолчанию — красный

    // сколько секунд осталось
    int secondsLeft = 0;                           // Счётчик оставшихся секунд в текущей фазе

    // длительность каждой фазы
    const int RED_SEC = 7;                         // Длительность красной фазы (сек)
    const int YELLOW_SEC = 2;                      // Длительность жёлтой фазы (сек)
    const int GREEN_SEC = 6;                       // Длительность зелёной фазы (сек)

    public Valgusfoor()                            // Конструктор страницы
    {                                              // Начало конструктора
        InitializeComponent();                     // Инициализация компонентов из XAML (создаёт элементы UI)

        StartClock();                              // Запускаю часы (тик раз в секунду, отображение HH:mm:ss)
        PaintOff();                                // Сбрасываю цвета: все круги серые, рамки прозрачные
    }                                              // Конец конструктора

    // ===== кнопки =====

    void OnSisseClicked(object sender, EventArgs e) // Обработчик кнопки «Включить»
    {
        isOn = true;                               // Помечаем светофор как включённый
        HeaderLabel.Text = "Светофор включен";     // Обновляем заголовок/статус

        if (isNight) StartNightBlink();            // Если выбран ночной режим — запускаем мигание
        else StartDayCycle();                      // Иначе — обычный дневной цикл фаз
    }

    void OnValjaClicked(object sender, EventArgs e) // Обработчик кнопки «Выключить»
    {
        isOn = false;                              // Помечаем светофор как выключенный
        HeaderLabel.Text = "Светофор выключен";    // Обновляем статус

        StopDayCycle();                            // Останавливаем дневной таймер (если был)
        StopNightBlink();                          // Останавливаем ночной таймер (если был)
        PaintOff();                                // Сбрасываем индикацию (все серые, чистые счётчики)
    }

    // ===== переключатель день/ночь =====

    void OnNightToggled(object sender, ToggledEventArgs e) // Обработчик тумблера «Ночной режим»
    {
        isNight = e.Value;                         // Запоминаем выбранное состояние (true = ночь)

        if (!isOn)                                 // Если светофор сейчас выключен
        {
            PaintOff();                            // Просто очистить индикацию
            HeaderLabel.Text =                     // Показать, какой режим выбран, но устройство выключено
                isNight ? "Ночной режим (включен)" : "Дневной режим (включен)";
            return;                                // Выходим — ничего не запускаем
        }

        if (isNight)                               // Если переключили в ночь при включённом светофоре
        {
            StopDayCycle();                        // Остановить дневной цикл
            StartNightBlink();                     // Запустить мигание жёлтого
        }
        else                                       // Переключили в день
        {
            StopNightBlink();                      // Остановить ночное мигание
            StartDayCycle();                       // Запустить дневной цикл фаз
        }
    }

    // ===== дневной цикл =====

    void StartDayCycle()                           // Запуск дневного цикла
    {
        if (dayTimer == null)                      // Если таймер ещё не создан
        {
            dayTimer = Dispatcher.CreateTimer();   // Создаём таймер, привязанный к UI-диспетчеру
        }

        dayTimer.Interval = TimeSpan.FromSeconds(1); // Интервал тика таймера — 1 секунда
        dayTimer.Tick -= OnDayTick;                // На всякий случай снимаем обработчик (чтобы не продублировать)
        dayTimer.Tick += OnDayTick;                // Подписываемся на событие тика

        current = Phase.Red;                       // Начинаем с красной фазы
        secondsLeft = RED_SEC;                     // Устанавливаем счётчик времени для красной
        PaintDayPhase();                           // Прокрашиваем активный круг и сбрасываем остальные
        UpdateCountdown();                         // Обновляем цифры обратного отсчёта на UI

        if (!dayTimer.IsRunning)                   // Если таймер ещё не запущен
            dayTimer.Start();                      // Запускаем

        HeaderLabel.Text = "Дневной режим";        // Обновляем статус на экране
    }

    void StopDayCycle()                            // Остановка дневного цикла
    {
        if (dayTimer != null)                      // Если таймер существует
            dayTimer.Stop();                       // Остановить тики

        ClearCountdowns();                         // Очистить числа обратного отсчёта
    }

    void OnDayTick(object? sender, EventArgs e)    // Обработчик тика дневного таймера (каждую сек.)
    {
        if (!isOn || isNight) return;              // Защита: если выключили или переключили на ночь — ничего не делаем

        secondsLeft--;                             // Уменьшаем оставшиеся секунды
        UpdateCountdown();                         // Обновляем отображение счётчика

        if (secondsLeft <= 0)                      // Когда время фазы вышло
        {
            if (current == Phase.Red) current = Phase.Yellow;   // Красный → Жёлтый
            else if (current == Phase.Yellow) current = Phase.Green; // Жёлтый → Зелёный
            else current = Phase.Red;                             // Зелёный → Красный

            if (current == Phase.Red) secondsLeft = RED_SEC;     // Назначаем длительность новой фазы
            else if (current == Phase.Yellow) secondsLeft = YELLOW_SEC;
            else if (current == Phase.Green) secondsLeft = GREEN_SEC;

            PaintDayPhase();                      // Обновляем цвета кругов под текущую фазу
            UpdateCountdown();                    // Перерисовываем цифры таймера
        }
    }

    void PaintDayPhase()                           // Прорисовка кругов в дневном режиме
    {
        // всё серое
        RedCircle.BackgroundColor = Colors.Gray;   // Сбрасываем красный
        YellowCircle.BackgroundColor = Colors.Gray;// Сбрасываем жёлтый
        GreenCircle.BackgroundColor = Colors.Gray; // Сбрасываем зелёный

        // рамки выключаю
        RedCircle.BorderColor = Colors.Transparent;   // Прозрачная рамка у красного
        YellowCircle.BorderColor = Colors.Transparent;// Прозрачная рамка у жёлтого
        GreenCircle.BorderColor = Colors.Transparent; // Прозрачная рамка у зелёного

        // активный круг подсвечиваю
        if (current == Phase.Red)                  // Если активен красный
        {
            RedCircle.BackgroundColor = Colors.Red;   // Красим фон в красный
            RedCircle.BorderColor = Colors.White;     // Белая рамка для акцента
        }
        else if (current == Phase.Yellow)          // Если активен жёлтый
        {
            YellowCircle.BackgroundColor = Colors.Yellow; // Фон — жёлтый
            YellowCircle.BorderColor = Colors.White;      // Белая рамка
        }
        else if (current == Phase.Green)           // Если активен зелёный
        {
            GreenCircle.BackgroundColor = Colors.Green;   // Фон — зелёный
            GreenCircle.BorderColor = Colors.White;       // Белая рамка
        }
    }

    // ===== ночной режим =====

    bool yellowOn = false;                         // Текущее состояние жёлтого при мигании (вкл/выкл)

    void StartNightBlink()                         // Запуск мигания жёлтого (ночь)
    {
        // красный и зелёный выключаю
        RedCircle.BackgroundColor = Colors.Gray;   // Гасим красный
        GreenCircle.BackgroundColor = Colors.Gray; // Гасим зелёный
        RedCircle.BorderColor = Colors.Transparent;// Рамка красного — прозрачная
        GreenCircle.BorderColor = Colors.Transparent;// Рамка зелёного — прозрачная

        ClearCountdowns();                         // В ночном режиме счётчики не показываем

        if (nightTimer == null)                    // Создаем таймер, если его ещё нет
        {
            nightTimer = Dispatcher.CreateTimer(); // Таймер под UI-диспетчером
        }

        nightTimer.Interval = TimeSpan.FromSeconds(1); // Мигание раз в секунду
        nightTimer.Tick -= OnNightTick;            // Снимаем обработчик во избежание дубля
        nightTimer.Tick += OnNightTick;            // Подписываемся на событие тика

        yellowOn = false;                          // Начинаем с «выключенного» жёлтого

        if (!nightTimer.IsRunning)                 // Если таймер ещё не запущен
            nightTimer.Start();                    // Запускаем мигание

        HeaderLabel.Text = "Ночной режим: мигает жёлтый"; // Обновляем статус
    }

    void StopNightBlink()                          // Остановка ночного режима
    {
        if (nightTimer != null)                    // Если ночной таймер существует
            nightTimer.Stop();                     // Останавливаем его

        YellowCircle.BackgroundColor = Colors.Gray;// Гасим жёлтый
        YellowCircle.BorderColor = Colors.Transparent; // Убираем рамку
    }

    void OnNightTick(object? sender, EventArgs e)  // Обработчик тика ночного таймера
    {
        if (!isOn || !isNight) return;             // Если выключили или вышли из ночного режима — выходим

        yellowOn = !yellowOn;                      // Инвертируем состояние (вкл/выкл) — эффект мигания

        if (yellowOn)                              // Если «включено»
        {
            YellowCircle.BackgroundColor = Colors.Yellow; // Зажигаем жёлтый
            YellowCircle.BorderColor = Colors.White;      // Даём белую рамку
        }
        else                                       // Если «выключено»
        {
            YellowCircle.BackgroundColor = Colors.Gray;   // Гасим жёлтый
            YellowCircle.BorderColor = Colors.Transparent;// Убираем рамку
        }
    }

    // ===== вспомогательные методы =====

    void PaintOff()                                // Универсальный сброс индикации
    {
        RedCircle.BackgroundColor = Colors.Gray;   // Серый красный круг
        YellowCircle.BackgroundColor = Colors.Gray;// Серый жёлтый круг
        GreenCircle.BackgroundColor = Colors.Gray; // Серый зелёный круг

        RedCircle.BorderColor = Colors.Transparent;// Без рамки
        YellowCircle.BorderColor = Colors.Transparent;// Без рамки
        GreenCircle.BorderColor = Colors.Transparent;// Без рамки

        ClearCountdowns();                         // Чистим цифры
    }

    void UpdateCountdown()                         // Обновление отображения обратного отсчёта
    {
        ClearCountdowns();                         // Сначала очистим все поля

        string t = secondsLeft.ToString();         // Текст текущего оставшегося времени

        if (current == Phase.Red) RedCountdown.Text = t;     // Пишем в красный счётчик
        else if (current == Phase.Yellow) YellowCountdown.Text = t; // Пишем в жёлтый
        else if (current == Phase.Green) GreenCountdown.Text = t;   // Пишем в зелёный
    }

    void ClearCountdowns()                         // Очистка всех счётчиков секунд
    {
        RedCountdown.Text = "";                    // Пусто у красного
        YellowCountdown.Text = "";                 // Пусто у жёлтого
        GreenCountdown.Text = "";                  // Пусто у зелёного
    }

    void StartClock()                              // Запуск часов (отдельный таймер)
    {
        if (clockTimer == null)                    // Если таймера ещё нет
        {
            clockTimer = Dispatcher.CreateTimer(); // Создаём таймер для UI-потока
        }

        clockTimer.Interval = TimeSpan.FromSeconds(1); // Тик раз в секунду
        clockTimer.Tick += (_, __) =>              // Лямбда-обработчик: параметры не используем
        {
            ClockLabel.Text = DateTime.Now.ToString("HH:mm:ss"); // Обновление текста лейбла текущим временем
        };

        if (!clockTimer.IsRunning)                 // Если часы ещё не идут
            clockTimer.Start();                    // Запускаем
    }
}                                                  // Конец класса/файла
