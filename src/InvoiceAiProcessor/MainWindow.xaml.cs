using System.Windows;

namespace InvoiceAiProcessor;

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