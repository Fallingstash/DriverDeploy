using DriverDeploy.Shared.Models;
using DriverDeploy.Shared.Services;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace DriverDeploy.Server
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<MachineInfo> Machines { get; } = new ObservableCollection<MachineInfo>();
        public ObservableCollection<DriverInfo> CurrentMachineDrivers { get; } = new ObservableCollection<DriverInfo>();
        public ObservableCollection<DriverPackage> DriverPackages { get; } = new ObservableCollection<DriverPackage>();

        private MachineInfo _selectedMachine;

        public MainWindow()
        {
            InitializeComponent();
            MachinesListView.ItemsSource = Machines;
            DriversListView.ItemsSource = CurrentMachineDrivers;
            DriverPackagesListView.ItemsSource = DriverPackages;

            // Загружаем тестовые пакеты драйверов
            LoadDriverPackages();
        }

        private void LoadDriverPackages()
        {
            DriverPackages.Clear();
            DriverPackages.Add(new DriverPackage
            {
                Name = "NVIDIA Graphics Driver",
                Version = "456.71",
                Description = "Драйвер для видеокарт NVIDIA",
                Size = 650000000
            });
            DriverPackages.Add(new DriverPackage
            {
                Name = "Realtek Audio Driver",
                Version = "6.0.1.1234",
                Description = "Драйвер для аудиочипов Realtek",
                Size = 120000000
            });
            DriverPackages.Add(new DriverPackage
            {
                Name = "Intel Network Adapter",
                Version = "12.15.0.5",
                Description = "Драйвер для сетевых адаптеров Intel",
                Size = 45000000
            });
        }

        private async void ScanButton_Click(object sender, RoutedEventArgs e)
        {
            ScanButton.IsEnabled = false;
            StatusText.Text = "Сканирование сети...";
            ScanProgress.Visibility = Visibility.Visible;
            Machines.Clear();

            try
            {
                var ipRange = MachineScanner.GetIPRangeWithLocalhost();
                StatusText.Text = $"Сканируем {ipRange.Count} адресов...";

                int foundCount = 0;

                var options = new ParallelOptions { MaxDegreeOfParallelism = 10 };
                await Task.Run(() =>
                {
                    Parallel.ForEach(ipRange, options, ip =>
                    {
                        var pingTask = MachineScanner.IsMachineOnline(ip);
                        pingTask.Wait();

                        if (pingTask.Result)
                        {
                            var checkTask = CheckForAgent(ip);
                            checkTask.Wait();

                            if (checkTask.Result != null)
                            {
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
            catch (Exception ex)
            {
                StatusText.Text = $"Ошибка сканирования: {ex.Message}";
            }
            finally
            {
                ScanProgress.Visibility = Visibility.Collapsed;
                ScanButton.IsEnabled = true;
            }
        }

        private async void ScanDriversButton_Click(object sender, RoutedEventArgs e)
        {
            if (MachinesListView.SelectedItem is MachineInfo selectedMachine)
            {
                await ScanDriversForMachine(selectedMachine);
            }
            else
            {
                ResultText.Text = "❌ Выберите машину из списка для сканирования драйверов";
            }
        }

        private async Task ScanDriversForMachine(MachineInfo machine)
        {
            try
            {
                StatusText.Text = $"🔍 Сканируем драйверы на {machine.MachineName}...";
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(5);

                var response = await client.GetAsync($"http://{machine.IpAddress}:8080/api/drivers");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var drivers = JsonConvert.DeserializeObject<DriverInfo[]>(json);

                    machine.InstalledDrivers.Clear();
                    foreach (var driver in drivers)
                    {
                        machine.InstalledDrivers.Add(driver);
                    }

                    // Обновляем список драйверов для выбранной машины
                    if (machine == _selectedMachine)
                    {
                        CurrentMachineDrivers.Clear();
                        foreach (var driver in drivers)
                        {
                            CurrentMachineDrivers.Add(driver);
                        }
                    }

                    ResultText.Text = $"✅ Найдено {drivers.Length} драйверов на {machine.MachineName}";
                    StatusText.Text = $"Готово: {drivers.Length} драйверов на {machine.MachineName}";
                }
                else
                {
                    ResultText.Text = $"❌ Ошибка при сканировании драйверов: {response.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                ResultText.Text = $"❌ Ошибка сканирования драйверов: {ex.Message}";
            }
        }

        private async void RefreshDriversButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedMachine != null)
            {
                await ScanDriversForMachine(_selectedMachine);
            }
            else
            {
                ResultText.Text = "❌ Сначала выберите машину из списка";
            }
        }

        private async void CheckUpdatesButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedMachine != null)
            {
                await CheckForDriverUpdates(_selectedMachine);
            }
            else
            {
                ResultText.Text = "❌ Сначала выберите машину из списка";
            }
        }

        private async Task CheckForDriverUpdates(MachineInfo machine)
        {
            try
            {
                StatusText.Text = $"🔍 Проверяем обновления на {machine.MachineName}...";
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(5);

                var response = await client.GetAsync($"http://{machine.IpAddress}:8080/api/drivers/outdated");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var outdatedDrivers = JsonConvert.DeserializeObject<DriverInfo[]>(json);

                    machine.OutdatedDrivers.Clear();
                    foreach (var driver in outdatedDrivers)
                    {
                        machine.OutdatedDrivers.Add(driver);
                    }

                    ResultText.Text = $"🔔 Найдено {outdatedDrivers.Length} устаревших драйверов на {machine.MachineName}";
                    StatusText.Text = $"Обновления: {outdatedDrivers.Length} драйверов требуют внимания";

                    // Показываем устаревшие драйверы в списке
                    if (machine == _selectedMachine)
                    {
                        foreach (var driver in CurrentMachineDrivers)
                        {
                            driver.NeedsUpdate = outdatedDrivers.Any(od => od.DeviceName == driver.DeviceName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ResultText.Text = $"❌ Ошибка проверки обновлений: {ex.Message}";
            }
        }

        private async void DeployDriverButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedMachine == null)
            {
                ResultText.Text = "❌ Выберите машину для развертывания";
                return;
            }

            if (DriverPackagesListView.SelectedItem is DriverPackage selectedPackage)
            {
                await DeployDriverToMachine(_selectedMachine, selectedPackage);
            }
            else
            {
                ResultText.Text = "❌ Выберите пакет драйвера для установки";
            }
        }

        private async Task DeployDriverToMachine(MachineInfo machine, DriverPackage driverPackage)
        {
            try
            {
                StatusText.Text = $"🚀 Устанавливаем {driverPackage.Name} на {machine.MachineName}...";
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(30); // Увеличиваем таймаут для установки

                var json = JsonConvert.SerializeObject(driverPackage);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"http://{machine.IpAddress}:8080/api/drivers/install", content);
                if (response.IsSuccessStatusCode)
                {
                    var resultJson = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<InstallationResult>(resultJson);

                    if (result.Success)
                    {
                        ResultText.Text = $"✅ {result.Message}";
                        StatusText.Text = $"Успешно установлен {driverPackage.Name}";
                    }
                    else
                    {
                        ResultText.Text = $"⚠️ {result.Message}";
                    }
                }
                else
                {
                    ResultText.Text = $"❌ Ошибка установки: {response.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                ResultText.Text = $"❌ Ошибка развертывания: {ex.Message}";
            }
        }

        private void InstallDriverButton_Click(object sender, RoutedEventArgs e)
        {
            ResultText.Text = "🔧 Функция установки драйвера - в разработке";
        }

        private void UpdateDrivers_Click(object sender, RoutedEventArgs e)
        {
            if (MachinesListView.SelectedItem is MachineInfo selectedMachine)
            {
                ResultText.Text = $"🔄 Выбрана машина: {selectedMachine.MachineName} для массового обновления драйверов";
                // Здесь будет массовое обновление всех устаревших драйверов
            }
            else
            {
                ResultText.Text = "❌ Выберите машину из списка для обновления драйверов";
            }
        }

        private void DebugButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var ipRange = MachineScanner.GetIPRangeWithLocalhost();
                var localIP = Dns.GetHostAddresses(Dns.GetHostName())
                    .FirstOrDefault(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);

                ResultText.Text = $"🔧 Диагностика:\n" +
                                 $"Имя компьютера: {Environment.MachineName}\n" +
                                 $"Локальный IP: {localIP}\n" +
                                 $"Диапазон сканирования: {string.Join(", ", ipRange.Take(5))}...\n" +
                                 $"Всего адресов: {ipRange.Count}\n" +
                                 $"Найдено машин: {Machines.Count}\n" +
                                 $"Пакеты драйверов: {DriverPackages.Count}";
            }
            catch (Exception ex)
            {
                ResultText.Text = $"❌ Ошибка диагностики: {ex.Message}";
            }
        }

        private async void MachinesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MachinesListView.SelectedItem is MachineInfo machine)
            {
                _selectedMachine = machine;
                SelectedMachineText.Text = $"{machine.MachineName} ({machine.IpAddress})";

                // Автоматически загружаем драйверы при выборе машины
                await ScanDriversForMachine(machine);
            }
        }

        private async Task<MachineInfo?> CheckForAgent(string ip)
        {
            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(2);

                var response = await client.GetAsync($"http://{ip}:8080/api/ping");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var machineInfo = JsonConvert.DeserializeObject<MachineInfo>(json);
                    return machineInfo;
                }
            }
            catch
            {
                // Игнорируем ошибки - значит агента нет на этой машине
            }
            return null;
        }
    }
}