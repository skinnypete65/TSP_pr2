using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Serilog;


namespace SystemResourceMonitor
{
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    class Program
    {
        static async Task Main(string[] args)
        {
            // Настройка конфигурации
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional:  false, reloadOnChange:  true)
                .Build();

            // Настройка Serilog
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
            .WriteTo.File(
                    path: configuration.GetSection("MonitoringSettings")["LogFilePath"],
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit:  7,
                    outputTemplate:  "{Timestamp. yyyy-MM-dd HH. mm. ss} [{Level}] {Message}{NewLine}{Exception}")
                .CreateLogger();

            Log.Information("=== Служба мониторинга системных ресурсов запущена ===");

            // Инициализация сервисов
            var monitor = new ResourceMonitor();
            var alertService = new AlertService(configuration);

            // Чтение настроек
            int interval = int.Parse(configuration.GetSection("MonitoringSettings")["IntervalSeconds"]);
            float cpuThreshold = float.Parse(
                configuration.GetSection("MonitoringSettings")
                .GetSection("Thresholds")
                ["Cpu"]
                );
            float memoryThreshold = float.Parse(
                configuration.GetSection("MonitoringSettings")
                .GetSection("Thresholds")
                ["Memory"]
                );

            // Обработка остановки приложения
            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                Log.Information("Инициирован процесс завершения...");
                eventArgs.Cancel = true;
                cts.Cancel();
            };

            try
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    // Сбор данных
                    float cpu = monitor.GetCpuUsage();
                    float memory = monitor.GetAvailableMemory();
                    float disk = monitor.GetDiskUsage();
                    float network = monitor.GetNetworkUsage();

                    // Формирование сообщения для лога
                    string logMessage = $"CPU.  {cpu:F2}% | Память.  {memory} MB | Диск.  {disk:F2}% | Сеть.  {network / 1024 / 1024:F2} MB/s";
                    Log.Information(logMessage);
                    Console.WriteLine($"{DateTime.Now}.  {logMessage}");

                    // Проверка порогов и отправка оповещений
                    if (cpu > cpuThreshold)
                    {
                        string subject = "Оповещение.  Высокая загрузка CPU";
                        string body = $"Загрузка CPU превысила порог в {cpuThreshold}%.\nТекущая загрузка.  {cpu:F2}%.";
                        alertService.SendEmail(subject, body);
                    }

                    if (memory < memoryThreshold)
                    {
                        string subject = "Оповещение.  Низкое количество доступной памяти";
                        string body = $"Доступная память опустилась ниже порога в {memoryThreshold} MB.\nТекущая доступная память.  {memory} MB.";
                        alertService.SendEmail(subject, body);
                    }

                    // Ожидание до следующего сбора данных
                    await Task.Delay(TimeSpan.FromSeconds(interval), cts.Token);
                }
            }
            catch (TaskCanceledException)
            {
                // Ожидаемое исключение при отмене задачи
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Произошла неожиданная ошибка.");
                Console.WriteLine($"[ERROR] {ex.Message}");
            }
            finally
            {
                Log.Information("=== Служба мониторинга системных ресурсов остановлена ===");
                Log.CloseAndFlush();
            }
        }
    }


}