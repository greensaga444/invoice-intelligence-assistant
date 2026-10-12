using System.Windows;

namespace InvoiceDownloader;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow(string statusMessage)
    {
        InitializeComponent();
        StatusText.Text = statusMessage;
    }
}