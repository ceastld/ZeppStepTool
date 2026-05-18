using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using ZeppStepTool.Wpf.Services;

namespace ZeppStepTool.Wpf
{
    public partial class MainWindow : Window
    {
        private readonly ZeppStepService _stepService = new ZeppStepService();
        private readonly CredentialStore _credentialStore = new CredentialStore();

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (_credentialStore.TryLoad(out var account, out var password))
            {
                AccountTextBox.Text = account;
                PasswordBox.Password = password;
            }
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveCredentials();
        }

        private void SaveCredentials()
        {
            var account = AccountTextBox.Text.Trim();
            if (account.Length == 0)
            {
                return;
            }

            _credentialStore.Save(account, PasswordBox.Password);
        }

        private async void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(StepsTextBox.Text.Trim(), out var steps))
            {
                SetStatus("请输入有效的步数。", false);
                return;
            }

            SubmitButton.IsEnabled = false;
            SetStatus("正在提交，请稍候…", null);

            try
            {
                var result = await _stepService.SubmitStepsAsync(
                    AccountTextBox.Text,
                    PasswordBox.Password,
                    steps).ConfigureAwait(true);

                SetStatus(result.Message, result.Success);
            }
            catch (Exception ex)
            {
                SetStatus("提交异常：" + ex.Message, false);
            }
            finally
            {
                SaveCredentials();
                SubmitButton.IsEnabled = true;
            }
        }

        private void SetStatus(string message, bool? success)
        {
            StatusTextBlock.Text = message;
            if (success == true)
            {
                StatusTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(0x1B, 0x7A, 0x3D));
            }
            else if (success == false)
            {
                StatusTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(0xB3, 0x26, 0x1E));
            }
            else
            {
                StatusTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55));
            }
        }
    }
}
