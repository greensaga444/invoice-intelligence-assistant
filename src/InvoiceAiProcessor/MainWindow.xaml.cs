using System.Windows;
using InvoiceIntelligence.Core.Processor;

namespace InvoiceAiProcessor;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow(
        string statusMessage,
        IReadOnlyList<InvoiceDecisionResult> decisionResults,
        ManualKnowledgeFeedbackService? feedbackService)
    {
        InitializeComponent();
        StatusText.Text = statusMessage;
    }
}