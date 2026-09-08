internal sealed class ConsoleSpinner : IDisposable
{
    private readonly CancellationTokenSource cancellationTokenSource = new();
    private readonly Task animationTask;
    private readonly int left;
    private readonly int top;

    public ConsoleSpinner(string text = "Thinking")
    {
        left = Console.CursorLeft;
        top = Console.CursorTop;

        animationTask = Task.Run(async () =>
        {
            //string[] frames = ["●○○", "○●○", "○○●", "○●○"];
            string[] frames = ["⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏"];
            int index = 0;

            while (!cancellationTokenSource.Token.IsCancellationRequested)
            {
                Console.SetCursorPosition(left, top);
                Console.Write($"{text} {frames[index]}");

                index = (index + 1) % frames.Length;

                try
                {
                    await Task.Delay(240, cancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        });
    }

    public void Dispose()
    {
        cancellationTokenSource.Cancel();

        try
        {
            animationTask.Wait();
        }
        catch (AggregateException)
        {
        }

        Console.SetCursorPosition(left, top);
        Console.Write(new string(' ', Console.WindowWidth - left - 1));
        Console.SetCursorPosition(left, top);
    }
}