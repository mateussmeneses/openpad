using System.ComponentModel;
using System.Windows;
using OpenPad.App.ViewModels;

namespace OpenPad.App;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel();
        DataContext = _viewModel;
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        // Garante liberação de todas as teclas e parada segura de threads ao fechar a janela
        _viewModel.Dispose();
        base.OnClosing(e);
    }
}
