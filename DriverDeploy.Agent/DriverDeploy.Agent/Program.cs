using System;
using DriverDeploy.Shared;
using Microsoft.AspNetCore.Builder;

namespace DriverDeploy.Agent {
  class Program {
    static void Main(string[] args) {

      var builder = WebApplication.CreateBuilder(args);
      var app = builder.Build();

      // Простой endpoint для проверки, что агент жив
      app.MapGet("/api/ping", () =>
      {
        Console.WriteLine("Получили ping запрос!");
        return new MachineInfo {
          Name = Environment.MachineName,
          IpAddress = "192.168.1.100", // пока заглушка
          Status = "Online"
        };
      });

      app.Run("http://localhost:8080");
    }
  }
}