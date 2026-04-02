public class VideoCleanupService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var files = Directory.GetFiles("videos");

            foreach (var file in files)
            {
                if (File.GetCreationTime(file) < DateTime.Now.AddMinutes(-1))
                {
                    File.Delete(file);
                }
            }

            await Task.Delay(30000);
        }
    }
}