namespace Story.Player.WinForms.Forms;

/// <summary>
/// Gestionează tema light/dark a aplicației Player.
/// </summary>
public static class ThemeManager
{
    public static bool IsDark { get; private set; } = true;

    // Dark theme
    public static Color BgPrimary   => IsDark ? Color.FromArgb(18, 18, 28) : Color.FromArgb(245, 243, 235);
    public static Color BgSecondary => IsDark ? Color.FromArgb(10, 10, 18) : Color.FromArgb(230, 225, 210);
    public static Color BgDecision  => IsDark ? Color.FromArgb(26, 24, 40) : Color.FromArgb(220, 215, 200);
    public static Color BgDecHover  => IsDark ? Color.FromArgb(44, 41, 65) : Color.FromArgb(200, 192, 170);
    public static Color BgBadge     => IsDark ? Color.FromArgb(35, 32, 18) : Color.FromArgb(210, 195, 140);
    public static Color BgHud       => IsDark ? Color.FromArgb(10, 10, 18) : Color.FromArgb(225, 220, 205);

    public static Color TextPrimary  => IsDark ? Color.FromArgb(220, 215, 200) : Color.FromArgb(40, 35, 25);
    public static Color TextSecond   => IsDark ? Color.FromArgb(150, 145, 130) : Color.FromArgb(100, 90, 70);
    public static Color TextAccent   => IsDark ? Color.FromArgb(255, 215, 100) : Color.FromArgb(140, 100, 20);
    public static Color TextBlockId  => IsDark ? Color.FromArgb(65, 62, 80)   : Color.FromArgb(160, 150, 130);
    public static Color ArrowNormal  => IsDark ? Color.FromArgb(70, 65, 55)   : Color.FromArgb(160, 150, 130);
    public static Color ArrowHover   => IsDark ? Color.FromArgb(255, 215, 100): Color.FromArgb(140, 100, 20);

    public static void Toggle() => IsDark = !IsDark;
}
