using System;
using System.Collections.Generic;

namespace DriverDeploy.Shared.Models
{
    public class DriverPackage
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<HardwareId> SupportedHardware { get; set; } = new();
        public string FilePath { get; set; } = string.Empty;
        public long Size { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }

    public class HardwareId
    {
        public string Vendor { get; set; } = string.Empty;
        public string Device { get; set; } = string.Empty;
        public string HardwareID { get; set; } = string.Empty;
    }
}