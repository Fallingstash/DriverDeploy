using System.Net.Http;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DriverDeploy.Server {
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window {
    public MainWindow() {
      InitializeComponent();
    }

    private async void ScanButton_Click(object sender, RoutedEventArgs e) {
      try {
        using var client = new HttpClient();
        // Пока тестируем на localhost - своём же компьютере
        var response = await client.GetAsync("http://localhost:8080/api/ping");
        var json = await response.Content.ReadAsStringAsync();
        ResultText.Text = $"Получили ответ: {json}";
      }
      catch (Exception ex) {
        ResultText.Text = $"Ошибка: {ex.Message}";
      }
    }
  }
}