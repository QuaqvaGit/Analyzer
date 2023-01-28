using System;
using System.Windows;
using System.Windows.Media;

namespace Analyzer
{
    public delegate void MessageSender(string message);
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        private void ClearSemantics()
        {
            Identifiers.Items.Clear();
            Constants.Items.Clear();
        }
        private void AnalyzeBTN_Click(object sender, RoutedEventArgs e)
        {
            MessageSender mesSender = (message) =>
            {
                MessageLabel.Foreground = Brushes.Green;
                MessageLabel.Content = message;
                MessageLabel.Visibility = Visibility.Visible;
            };
            ClearSemantics();
            SyntaxAnalyzer analyzer = new SyntaxAnalyzer(InputTB.Text, mesSender);
            try
            {
                InputTB_GotFocus(sender, e);
                analyzer.Analyze();
                mesSender("Строка успешно прошла анализ");

                SemanticsTab.Visibility = Visibility.Visible;

                foreach (var identifier in analyzer.Identifiers)
                    Identifiers.Items.Add(identifier);

                analyzer.Constants.Sort();
                foreach (var constant in analyzer.Constants)
                    Constants.Items.Add(constant);
            }
            catch (Exception ex) {
                MessageLabel.Content = ex.Message;
                MessageLabel.Visibility = Visibility.Visible;
                MessageLabel.Foreground = Brushes.Red;
                //MessageBox.Show(ex.Message,"Ошибка",MessageBoxButton.OK,MessageBoxImage.Error);
                InputTB.Focus();
                InputTB.Select((int)analyzer.ErrorIndex, 1);
            }
        }

        private void InputTB_GotFocus(object sender, RoutedEventArgs e)
        {
            if (InputTB.Text == "Введите строку для анализа...")
                InputTB.Clear();
        }

        private void InputTB_LostFocus(object sender, RoutedEventArgs e)
        {
            if (InputTB.Text.Length == 0)
                InputTB.Text = "Введите строку для анализа...";
        }

        private void InputTB_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            SemanticsTab.Visibility = Visibility.Hidden;
            MessageLabel.Visibility = Visibility.Hidden;
            ClearSemantics();
        }
    }
}
