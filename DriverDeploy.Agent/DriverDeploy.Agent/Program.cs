using DriverDeploy.Shared.Models;
using Microsoft.AspNetCore.Builder;
using System;
using System.Net;
using System.Net.Sockets;

namespace DriverDeploy.Agent {
  class Program {
    static void Main(string[] args) {
      var builder = WebApplication.CreateBuilder(args);
      var app = builder.Build();

      app.MapGet("/api/ping", () =>
      {
        Console.WriteLine($"✅ Получен ping запрос от клиента");
        return new MachineInfo {
          MachineName = Environment.MachineName,
          IpAddress = GetLocalIPAddress(),
          Status = "Online"
        };
      });

      // 🔥 ВАЖНО: меняем localhost на 0.0.0.0
      app.Run("http://0.0.0.0:8080");  // ✅ Слушаем все сетевые интерфейсы
    }

    static string GetLocalIPAddress() {
      var host = Dns.GetHostEntry(Dns.GetHostName());
      foreach (var ip in host.AddressList) {
        if (ip.AddressFamily == AddressFamily.InterNetwork) {
          return ip.ToString();
        }
      }
      return "Unknown";
    }
  }
}