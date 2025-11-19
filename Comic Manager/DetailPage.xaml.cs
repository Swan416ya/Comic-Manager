using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic; // 用于 List
using System.Linq; // 用于 OrderBy 排序
using System.Threading.Tasks;
using Windows.Storage.Pickers; // 文件选择器

namespace Comic_Manager
{
    public sealed partial class DetailPage : Page
    {
        // 当前页面正在展示的漫画对象
        private ComicSeries _currentSeries;

        public DetailPage()
        {
            this.InitializeComponent();
        }

        // 1. 进入页面时加载数据
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is ComicSeries series)
            {
                _currentSeries = series;

                // 填充界面数据
                DetailTitle.Text = series.Title;
                DetailAuthor.Text = $"作者: {series.Author}";
                DetailCoverImage.Source = series.CoverImageBitmap;

                // 绑定章节列表
                ChaptersGridView.ItemsSource = series.Chapters;

                // 【新增】根据数据初始化下拉框的选中项
                SelectModeInComboBox(_currentSeries.Mode);
            }
        }

        // 辅助：根据数据设置下拉框选中项
        private void SelectModeInComboBox(ReadingMode mode)
        {
            // 遍历下拉框里的每一项，找到 Tag 和 mode 名字一样的
            foreach (ComboBoxItem item in ModeComboBox.Items)
            {
                if (item.Tag != null && item.Tag.ToString() == mode.ToString())
                {
                    ModeComboBox.SelectedItem = item;
                    break;
                }
            }
        }

        // 2. 当用户改变阅读模式时，保存到对象里
        private void OnModeChanged(object sender, SelectionChangedEventArgs e)
        {
            // 确保 _currentSeries 不为空（防止页面刚初始化时触发）
            if (_currentSeries != null && ModeComboBox.SelectedItem is ComboBoxItem item)
            {
                if (item.Tag != null && Enum.TryParse(item.Tag.ToString(), out ReadingMode newMode))
                {
                    _currentSeries.Mode = newMode;
                }
            }
        }

        // 3. 顶部返回按钮
        private void OnBackClick(object sender, RoutedEventArgs e)
        {
            if (this.Frame.CanGoBack)
            {
                this.Frame.GoBack();
            }
        }

        // 4. 点击添加章节按钮
        private async void OnAddChapterClick(object sender, RoutedEventArgs e)
        {
            await ShowAddChapterDialog();
        }

        // 5. 【核心修改】点击章节 -> 弹出新窗口阅读
        private void OnChapterItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is ComicChapter chapter)
            {
                // === 创建一个新的窗口 ===
                Window readingWindow = new Window();
                readingWindow.Title = $"{_currentSeries.Title} - {chapter.DisplayTitle}";

                // 新窗口的内容是一个 Frame，用来导航到 ReadingPage
                Frame rootFrame = new Frame();
                readingWindow.Content = rootFrame;

                // 准备传递的参数 (章节信息 + 阅读模式)
                var param = new Tuple<ComicChapter, ReadingMode>(chapter, _currentSeries.Mode);

                // 让 Frame 跳转到 ReadingPage
                rootFrame.Navigate(typeof(ReadingPage), param);

                // 激活（显示）新窗口
                readingWindow.Activate();
            }
        }

        // 6. 显示添加章节的弹窗 (逻辑保持不变)
        private async Task ShowAddChapterDialog()
        {
            StackPanel content = new StackPanel() { Spacing = 12 };

            TextBox numBox = new TextBox()
            {
                Header = "章节序号",
                PlaceholderText = "数字 (如 1, 1.5)",
                InputScope = new Microsoft.UI.Xaml.Input.InputScope()
                {
                    Names = { new Microsoft.UI.Xaml.Input.InputScopeName(Microsoft.UI.Xaml.Input.InputScopeNameValue.Number) }
                }
            };

            TextBlock pathText = new TextBlock() { Text = "尚未选择文件夹", Opacity = 0.6, FontSize = 12, TextWrapping = TextWrapping.Wrap };

            Button pickFolderBtn = new Button() { Content = "选择图片文件夹" };
            string selectedPath = "";

            // 文件夹选择逻辑
            pickFolderBtn.Click += async (s, args) =>
            {
                var picker = new FolderPicker();
                // 这里必须用主窗口的句柄，因为弹窗是依附于主窗口的
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

                picker.FileTypeFilter.Add("*");

                var folder = await picker.PickSingleFolderAsync();
                if (folder != null)
                {
                    selectedPath = folder.Path;
                    pathText.Text = selectedPath;
                    pathText.Opacity = 1.0;
                }
            };

            content.Children.Add(numBox);
            content.Children.Add(pickFolderBtn);
            content.Children.Add(pathText);

            ContentDialog dialog = new ContentDialog()
            {
                XamlRoot = this.XamlRoot,
                Title = "添加章节",
                PrimaryButtonText = "确定",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Primary,
                Content = content
            };

            // 循环校验逻辑
            while (true)
            {
                var result = await dialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
                {
                    // 校验 1: 数字
                    if (!double.TryParse(numBox.Text, out double chapterNum))
                    {
                        // 简单提示错误，不关闭弹窗（实际效果取决于 ContentDialog 行为，这里简化处理）
                        numBox.Header = "章节序号 (请输入有效数字!)";
                        continue;
                    }

                    // 校验 2: 路径
                    if (string.IsNullOrEmpty(selectedPath))
                    {
                        pathText.Text = "错误：必须选择一个文件夹！";
                        pathText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
                        continue;
                    }

                    // 保存数据
                    var newChapter = new ComicChapter()
                    {
                        ChapterNumber = chapterNum,
                        SourceFolderPath = selectedPath
                    };

                    _currentSeries.Chapters.Add(newChapter);
                    SortChapters(); // 排序

                    break;
                }
                else
                {
                    break;
                }
            }
        }

        // 辅助：章节排序
        private void SortChapters()
        {
            var sorted = _currentSeries.Chapters.OrderBy(c => c.ChapterNumber).ToList();
            _currentSeries.Chapters.Clear();
            foreach (var c in sorted)
            {
                _currentSeries.Chapters.Add(c);
            }
        }
    }
}