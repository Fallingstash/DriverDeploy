using DriverDeploy.Shared.Models;
using DriverDeploy.Shared.Services;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;

namespace DriverDeploy.Server {
  public partial class MainWindow : Window {
    // Коллекция для хранения найденных машин
    public ObservableCollection<MachineInfo> Machines { get; } = new ObservableCollection<MachineInfo>();

    public MainWindow() {
      InitializeComponent();
      // Привязываем список машин к интерфейсу
      MachinesListView.ItemsSource = Machines;
    }

    private async void ScanButton_Click(object sender, RoutedEventArgs e) {
      ScanButton.IsEnabled = false;
      StatusText.Text = "Сканирование сети...";
      Machines.Clear();

      try {
        // 🔥 Используем улучшенный метод с localhost
        var ipRange = MachineScanner.GetIPRangeWithLocalhost();
        StatusText.Text = $"Сканируем {ipRange.Count} адресов...";

        int foundCount = 0;

        // Используем Parallel.ForEach для ускорения (многопоточность)
        var options = new ParallelOptions { MaxDegreeOfParallelism = 10 };
        await Task.Run(() => {
          Parallel.ForEach(ipRange, options, ip =>
          {
            // Проверяем, онлайн ли машина (ping)
            var pingTask = MachineScanner.IsMachineOnline(ip);
            pingTask.Wait(); // Ждём завершения в этом потоке

            if (pingTask.Result) {
              // Пытаемся подключиться к агенту
              var checkTask = CheckForAgent(ip);
              checkTask.Wait(); // Ждём завершения

              if (checkTask.Result != null) {
                // Важно: обновляем UI в основном потоке
                Application.Current.Dispatcher.Invoke(() =>
                {
                  Machines.Add(checkTask.Result);
                  foundCount++;
                  StatusText.Text = $"Найдено машин: {foundCount}. Проверяем {ip}...";
                });
              }
            }
          });
        });

        StatusText.Text = $"Сканирование завершено. Найдено {foundCount} машин с агентом.";
      }
      catch (Exception ex) {
        StatusText.Text = $"Ошибка сканирования: {ex.Message}";
      }
      finally {
        ScanButton.IsEnabled = true;
      }
    }

    private async Task<MachineInfo?> CheckForAgent(string ip) {
      try {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(2); // Таймаут 2 секунды

        var response = await client.GetAsync($"http://{ip}:8080/api/ping");
        if (response.IsSuccessStatusCode) {
          var json = await response.Content.ReadAsStringAsync();
          var machineInfo = JsonConvert.DeserializeObject<MachineInfo>(json);
          return machineInfo;
        }
      }
      catch {
        // Игнорируем ошибки - значит агента нет на этой машине
      }
      return null;
    }

    private void UpdateDrivers_Click(object sender, RoutedEventArgs e) {
      if (MachinesListView.SelectedItem is MachineInfo selectedMachine) {
        ResultText.Text = $"Выбрана машина: {selectedMachine.MachineName} для обновления драйверов";
      } else {
        ResultText.Text = "Выберите машину из списка для обновления драйверов";
      }
    }

    private void DebugButton_Click(object sender, RoutedEventArgs e) {
      try {
        var ipRange = MachineScanner.GetIPRangeWithLocalhost();
        var localIP = Dns.GetHostAddresses(Dns.GetHostName())
            .FirstOrDefault(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);

        ResultText.Text = $"Диагностика:\n" +
                         $"Имя компьютера: {Environment.MachineName}\n" +
                         $"Локальный IP: {localIP}\n" +
                         $"Диапазон сканирования: {string.Join(", ", ipRange.Take(5))}...\n" +
                         $"Всего адресов: {ipRange.Count}";
      }
      catch (Exception ex) {
        ResultText.Text = $"Ошибка диагностики: {ex.Message}";
      }
    }
  }
}