using DriverDeploy.Shared.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace DriverDeploy.Shared.Services {
  public class DriverRepositoryService {
    private readonly HttpClient _httpClient;
    private readonly string _homeIp;
    private string httpIp;

    // Кэш mapping'а драйверов
    private RepoDriverMapping _cachedMapping;
    private DateTime _lastUpdateTime;

    public DriverRepositoryService(string homeIp) {
      _httpClient = new HttpClient();
      _homeIp = homeIp.TrimEnd('/');
      _httpClient.Timeout = TimeSpan.FromSeconds(30);
      EnsureServerUrlAsync();
    }

    private async Task EnsureServerUrlAsync() {
      httpIp = await DiscoverServerAsync();
    }

    private async Task<string> DiscoverServerAsync() {
      // Получаем локальный IP для определения подсети

      // Извлекаем базовую часть IP (например, "192.168.1.")
      string baseIp = _homeIp.Substring(0, _homeIp.LastIndexOf('.') + 1);

      // Поиск сервера в сети
      for (int i = 1; i <= 254; i++)  // Исправлено: 254 вместо 256 (последний октет 1-254)
      {
        var ip = baseIp + i.ToString();  // Добавляем номер хоста
        if (await PingServerAsync(ip))
          return ip;
      }
      throw new Exception("Сервер не найден в сети");
    }

    private async Task<bool> PingServerAsync(string ip) {
      try {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(2);

        var response = await client.GetAsync($"http://{ip}:80/drivers.json");
        return response.IsSuccessStatusCode;
      }
      catch {
        return false;
      }
    }

    /// <summary>
    /// Загружает mapping драйверов из репозитория
    /// </summary>
    public async Task<RepoDriverMapping> LoadDriverMappingAsync() {
      try {
        var json = await _httpClient.GetStringAsync($"{httpIp}/drivers.json");
        var mapping = JsonConvert.DeserializeObject<RepoDriverMapping>(json);

        if (mapping != null) {
          _cachedMapping = mapping;
          _lastUpdateTime = DateTime.Now;
        }

        return mapping ?? new RepoDriverMapping();
      }
      catch (Exception ex) {
        throw new Exception($"Не удалось загрузить drivers.json из репозитория: {ex.Message}");
      }
    }

    /// <summary>
    /// Находит подходящий драйвер для устройства по HardwareID
    /// </summary>
    public RepoDriverEntry? FindDriverForDevice(DeviceDescriptor device) {
      if (_cachedMapping?.Drivers == null)
        return null;

      foreach (var driver in _cachedMapping.Drivers) {
        if (IsDeviceCompatibleWithDriver(device, driver)) {
          return driver;
        }
      }

      return null;
    }

    /// <summary>
    /// Проверяет совместимость устройства с драйвером по HardwareID
    /// </summary>
    private bool IsDeviceCompatibleWithDriver(DeviceDescriptor device, RepoDriverEntry driver) {
      if (device.HardwareIds == null || driver.HardwareIds == null)
        return false;

      foreach (var deviceHwId in device.HardwareIds) {
        foreach (var driverHwId in driver.HardwareIds) {
          if (deviceHwId.IndexOf(driverHwId, StringComparison.OrdinalIgnoreCase) >= 0) {
            return true;
          }
        }
      }

      return false;
    }

    /// <summary>
    /// Преобразует запись из репозитория в DriverPackage для отправки агенту
    /// </summary>
    public DriverPackage ConvertToDriverPackage(RepoDriverEntry repoEntry) {
      return new DriverPackage {
        Name = repoEntry.Name,
        Version = repoEntry.Version,
        Description = repoEntry.Description,
        Url = $"{httpIp}/{repoEntry.Url.TrimStart('/')}",
        InstallArgs = repoEntry.InstallArgs,
        Sha256 = repoEntry.Sha256,
        FileName = System.IO.Path.GetFileName(repoEntry.Url)
      };
    }

    /// <summary>
    /// Обновляет кэш если прошло больше 5 минут
    /// </summary>
    public async Task RefreshCacheIfNeededAsync() {
      if (_cachedMapping == null || DateTime.Now - _lastUpdateTime > TimeSpan.FromMinutes(5)) {
        await LoadDriverMappingAsync();
      }
    }
  }
}