namespace CGC;

public class ProgressBar(int total, int barWidth = 40)
{
    private int _current;

    public void Update(int current)
    {
        _current = current;
        Draw();
    }

    private void Draw()
    {
        var percentage = (float)_current / total;
        var filled = (int)Math.Floor(percentage * barWidth);
        var empty = barWidth - filled;

        Console.CursorLeft = 0;
        Console.Write("[");
        Console.BackgroundColor = ConsoleColor.Green;
        Console.Write(new string(' ', filled));
        Console.BackgroundColor = ConsoleColor.Black;
        Console.Write(new string(' ', empty));
        Console.Write($"] {_current * 100 / total}%");
    }
}