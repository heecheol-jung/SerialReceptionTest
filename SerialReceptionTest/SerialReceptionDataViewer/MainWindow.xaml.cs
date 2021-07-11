using System.Windows;
using SerialReceptionDataViewer.AppWnd;

namespace SerialReceptionDataViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void BtnSerialReceptionDataViewer_Click(object sender, RoutedEventArgs e)
        {
            WndSerialReceptionDataViewer viewer1 = new WndSerialReceptionDataViewer();
            viewer1.ShowDialog();
        }
    }
}
