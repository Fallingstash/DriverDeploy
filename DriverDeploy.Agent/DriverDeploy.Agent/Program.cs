using DriverDeploy.Shared.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http; // Добавляем этот using для Results
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace DriverDeploy.Agent
{
    class Program
    {
        private static List<DriverInfo> _systemDrivers = new();

        static async Task Main(string[] args) // Меняем на async Task
        {
            var builder = WebApplication.CreateBuilder(args);
            var app = builder.Build();

            // Эндпоинт для проверки доступности
            app.MapGet("/api/ping", () =>
            {
                Console.WriteLine($"✅ Получен ping запрос от клиента");
                return new MachineInfo
                {
                    MachineName = Environment.MachineName,
                    IpAddress = GetLocalIPAddress(),
                    Status = "Online",
                    OSVersion = Environment.OSVersion.VersionString,
                    Architecture = Environment.Is64BitOperatingSystem ? "x64" : "x86",
                    IsOnline = true
                };
            });

            // Эндпоинт для получения списка драйверов
            app.MapGet("/api/drivers", () =>
            {
                Console.WriteLine($"📦 Запрос списка драйверов");
                if (!_systemDrivers.Any())
                {
                    _systemDrivers = ScanSystemDrivers();
                }
                return _systemDrivers;
            });

            // Эндпоинт для установки драйвера
            app.MapPost("/api/drivers/install", async (DriverPackage driverPackage) =>
            {
                Console.WriteLine($"🔧 Установка драйвера: {driverPackage.Name}");

                try
                {
                    var result = await InstallDriver(driverPackage);
                    return Results.Ok(result);
                }
                catch (Exception ex)
                {
                    return Results.Problem($"Ошибка установки: {ex.Message}");
                }
            });

            // Эндпоинт для проверки обновлений
            app.MapGet("/api/drivers/outdated", () =>
            {
                Console.WriteLine($"🔍 Проверка устаревших драйверов");
                var outdated = FindOutdatedDrivers();
                return Results.Ok(outdated);
            });

            await app.RunAsync("http://0.0.0.0:8080"); // Меняем на RunAsync
        }

        static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            return "Unknown";
        }

        static List<DriverInfo> ScanSystemDrivers()
        {
            return new List<DriverInfo>
            {
                new DriverInfo { DeviceName = "NVIDIA GeForce GTX 1060", DriverVersion = "456.71", Provider = "NVIDIA" },
                new DriverInfo { DeviceName = "Realtek Audio", DriverVersion = "6.0.1.1234", Provider = "Realtek" }
            };
        }

        static async Task<InstallationResult> InstallDriver(DriverPackage driverPackage)
        {
            await Task.Delay(2000);

            return new InstallationResult
            {
                Success = true,
                Message = $"Драйвер {driverPackage.Name} успешно установлен",
                DriverName = driverPackage.Name,
                MachineName = Environment.MachineName
            };
        }

        static List<DriverInfo> FindOutdatedDrivers()
        {
            return new List<DriverInfo>
            {
                new DriverInfo { DeviceName = "NVIDIA GeForce GTX 1060", DriverVersion = "456.71", Provider = "NVIDIA", NeedsUpdate = true }
            };
        }
    }
}