namespace Story.Player.WinForms.Forms;

/// <summary>
/// Generează sunete simple prin BeepPlayer (fără dependențe externe).
/// Redă un bip melodic la navigare, un alt ton la hover decizii.
/// </summary>
public static class StorySound
{
    public static bool Enabled { get; set; } = true;

    /// <summary>Sunet la trecerea la un bloc nou.</summary>
    public static void PlayNavigate()
    {
        if (!Enabled) return;
        Task.Run(() =>
        {
            try
            {
                Console.Beep(440, 60);
                Thread.Sleep(30);
                Console.Beep(550, 80);
            }
            catch { /* sunetul nu e disponibil pe toate sistemele */ }
        });
    }

    /// <summary>Sunet la alegerea unei decizii (click).</summary>
    public static void PlayDecision()
    {
        if (!Enabled) return;
        Task.Run(() =>
        {
            try
            {
                Console.Beep(660, 50);
                Thread.Sleep(20);
                Console.Beep(880, 60);
            }
            catch { }
        });
    }

    /// <summary>Sunet la finalul poveștii (bun).</summary>
    public static void PlayGoodEnd()
    {
        if (!Enabled) return;
        Task.Run(() =>
        {
            try
            {
                int[] notes = { 523, 659, 784, 1047 };
                foreach (var n in notes)
                {
                    Console.Beep(n, 100);
                    Thread.Sleep(20);
                }
            }
            catch { }
        });
    }

    /// <summary>Sunet la finalul poveștii (rău).</summary>
    public static void PlayBadEnd()
    {
        if (!Enabled) return;
        Task.Run(() =>
        {
            try
            {
                int[] notes = { 392, 349, 330, 294 };
                foreach (var n in notes)
                {
                    Console.Beep(n, 130);
                    Thread.Sleep(20);
                }
            }
            catch { }
        });
    }
}
