namespace DriverDeploy.Agent.Services {
  public class AgentConfiguration {
    public string TempDownloadPath { get; set; } = Path.Combine(Path.GetTempPath(), "DriverDeploy");
    public int DownloadTimeoutMinutes { get; set; } = 10;
    public int InstallationTimeoutMinutes { get; set; } = 5;
    public bool VerifyHash { get; set; } = true;
    public bool CleanupAfterInstall { get; set; } = true;
    public string[] AllowedFileExtensions { get; set; } = { ".msi", ".exe", ".inf" };

    // Настройки по умолчанию для разных типов установщиков
    public string DefaultMsiArgs { get; set; } = "/quiet /norestart";
    public string DefaultExeArgs { get; set; } = "/S /quiet";
    public string DefaultInfArgs { get; set; } = "/add-driver \"{0}\" /install";

    // Логирование
    public bool EnableDetailedLogging { get; set; } = true;
    public string LogFilePath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "DriverDeploy", "agent.log");
  }
}