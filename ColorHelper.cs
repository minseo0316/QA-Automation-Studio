using System.Drawing;

namespace MyWinFormsApp;

internal static class ColorHelper
{
    public static Color Lerp(Color from, Color to, float amount)
    {
        amount = Math.Clamp(amount, 0f, 1f);
        int r = (int)(from.R + (to.R - from.R) * amount);
        int g = (int)(from.G + (to.G - from.G) * amount);
        int b = (int)(from.B + (to.B - from.B) * amount);
        int a = (int)(from.A + (to.A - from.A) * amount);
        return Color.FromArgb(a, r, g, b);
    }
}
