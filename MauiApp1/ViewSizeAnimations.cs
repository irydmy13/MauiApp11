using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace MauiApp1;

public static class ViewSizeAnimations
{
    public static Task WidthRequestTo(this VisualElement v, double newWidth, uint length, Easing? easing = null)
    {
        double start = v.WidthRequest > 0 ? v.WidthRequest : v.Width;
        easing ??= Easing.Linear;

        var tcs = new TaskCompletionSource();
        v.Animate(
            name: "WidthRequestTo",
            callback: d => v.WidthRequest = d,
            start: start,
            end: newWidth,
            rate: 16,
            length: length,
            easing: easing,
            finished: (d, c) => tcs.SetResult()
        );
        return tcs.Task;
    }

    public static Task HeightRequestTo(this VisualElement v, double newHeight, uint length, Easing? easing = null)
    {
        double start = v.HeightRequest > 0 ? v.HeightRequest : v.Height;
        easing ??= Easing.Linear;

        var tcs = new TaskCompletionSource();
        v.Animate(
            name: "HeightRequestTo",
            callback: d => v.HeightRequest = d,
            start: start,
            end: newHeight,
            rate: 16,
            length: length,
            easing: easing,
            finished: (d, c) => tcs.SetResult()
        );
        return tcs.Task;
    }
}
