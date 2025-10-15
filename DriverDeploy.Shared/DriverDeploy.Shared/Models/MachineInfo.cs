using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DriverDeploy.Shared.Models {
  public class MachineInfo {
    public string? MachineName { get; set; } = string.Empty;
    public string? IpAddress { get; set; } = string.Empty;
    public string? Status { get; set; } = "Unknown";
    public DateTime LastSeen { get; set; } = DateTime.Now;
  }
}
