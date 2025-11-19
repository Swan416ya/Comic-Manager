using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
// 引入 System.Diagnostics 用于打开浏览器
using System.Diagnostics;

namespace Comic_Manager
{
    public sealed partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            this.InitializeComponent();

            // 在页面加载时，我们需要设置下拉框的默认值
            // 这里简单处理：默认让它显示第一项（跟随系统）
            // 如果你想做得更完美，需要保存用户的设置到本地文件，下次打开时读取
            ThemeComboBox.SelectedIndex = 0;
        }

        // 点击 GitHub 链接
        private void OnGithubLinkClick(object sender, RoutedEventArgs e)
        {
            // C# 打开网页的标准写法
            Process.Start(new ProcessStartInfo("https://github.com/Swan416ya")
            {
                UseShellExecute = true
            });
        }

        // 主题切换逻辑
        private void OnThemeChanged(object sender, SelectionChangedEventArgs e)
        {
            // 1. 获取选中的项
            var comboBox = sender as ComboBox;
            var selectedItem = comboBox.SelectedItem as ComboBoxItem;

            if (selectedItem != null && selectedItem.Tag != null)
            {
                string tag = selectedItem.Tag.ToString();
                ElementTheme newTheme = ElementTheme.Default;

                // 2. 根据 Tag 决定主题
                switch (tag)
                {
                    case "Light":
                        newTheme = ElementTheme.Light;
                        break;
                    case "Dark":
                        newTheme = ElementTheme.Dark;
                        break;
                    default:
                        newTheme = ElementTheme.Default;
                        break;
                }

                // 3. 【关键】获取主窗口的内容区域，并设置它的主题
                // XamlRoot.Content 实际上就是我们 MainWindow 里的那个 Grid
                if (this.XamlRoot != null && this.XamlRoot.Content is FrameworkElement rootElement)
                {
                    rootElement.RequestedTheme = newTheme;
                }
            }
        }
    }
}