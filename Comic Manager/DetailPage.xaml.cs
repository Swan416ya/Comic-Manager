using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.ObjectModel;
using System.Linq; // 用于排序
using System.Threading.Tasks;
using Windows.Storage.Pickers;

namespace Comic_Manager
{
    public sealed partial class DetailPage : Page
    {
        // 当前页面正在展示的这本漫画对象
        private ComicSeries _currentSeries;

        public DetailPage()
        {
            this.InitializeComponent();
        }

        // 1. 页面导航进入时，接收传递过来的漫画数据
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
            }
        }

        // 2. 返回按钮
        private void OnBackClick(object sender, RoutedEventArgs e)
        {
            if (this.Frame.CanGoBack)
            {
                this.Frame.GoBack();
            }
        }

        // 3. 点击添加章节
        private async void OnAddChapterClick(object sender, RoutedEventArgs e)
        {
            await ShowAddChapterDialog();
        }

        // 4. 点击章节列表中的某一项 (进入阅读页，暂时留空)
        private void OnChapterItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is ComicChapter chapter)
            {
                // 以后这里写：
                // Frame.Navigate(typeof(ReadingPage), chapter);

                // 暂时弹个窗提示一下
                ShowSimpleAlert($"准备阅读: {chapter.DisplayTitle}\n路径: {chapter.SourceFolderPath}");
            }
        }

        // === 核心：添加章节弹窗 ===
        private async Task ShowAddChapterDialog()
        {
            // 构建弹窗内容
            StackPanel content = new StackPanel() { Spacing = 12 };

            // 输入框：只允许输入数字
            TextBox numBox = new TextBox()
            {
                Header = "章节序号 (数字)",
                PlaceholderText = "例如: 1 或 1.5",
                InputScope = new Microsoft.UI.Xaml.Input.InputScope()
                {
                    Names = { new Microsoft.UI.Xaml.Input.InputScopeName(Microsoft.UI.Xaml.Input.InputScopeNameValue.Number) }
                }
            };

            // 文件夹路径显示
            TextBlock pathText = new TextBlock() { Text = "尚未选择文件夹", Opacity = 0.6, FontSize = 12, TextWrapping = TextWrapping.Wrap };

            // 选择文件夹按钮
            Button pickFolderBtn = new Button() { Content = "选择图片文件夹" };
            string selectedPath = "";

            pickFolderBtn.Click += async (s, args) =>
            {
                var picker = new FolderPicker();
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
                picker.FileTypeFilter.Add("*"); // 文件夹必须加这个

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

            // 循环显示弹窗，直到输入合法或者用户取消
            while (true)
            {
                var result = await dialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
                {
                    // 校验 1: 必须是数字
                    if (!double.TryParse(numBox.Text, out double chapterNum))
                    {
                        ShowErrorTip(numBox, "请输入有效的数字！");
                        continue; // 重新显示弹窗（实际上 WinUI Dialog 关闭后不能马上 reopen，这里简化逻辑，实际会关闭）
                        // 这里为了简单，如果没有校验通过，我们直接不保存，但在 UI 上最好给个提示
                        // 由于 ContentDialog 关闭就销毁了，我们用简单的方式：只在合法时退出
                    }

                    // 校验 2: 必须选文件夹
                    if (string.IsNullOrEmpty(selectedPath))
                    {
                        // 没选文件夹，不做任何事，或者你可以弹个警告
                        return;
                    }

                    // --- 一切正常，保存数据 ---

                    // 创建新章节对象
                    var newChapter = new ComicChapter()
                    {
                        ChapterNumber = chapterNum,
                        SourceFolderPath = selectedPath
                    };

                    _currentSeries.Chapters.Add(newChapter);

                    // 重新排序：让章节按 1, 2, 3 顺序排列
                    SortChapters();

                    break; // 退出循环
                }
                else
                {
                    break; // 用户点了取消
                }
            }
        }

        // 辅助方法：对章节进行排序
        private void SortChapters()
        {
            // ObservableCollection 没有直接的 Sort 方法，所以我们要倒腾一下
            var sorted = _currentSeries.Chapters.OrderBy(c => c.ChapterNumber).ToList();
            _currentSeries.Chapters.Clear();
            foreach (var c in sorted)
            {
                _currentSeries.Chapters.Add(c);
            }
        }

        // 辅助方法：简单的提示弹窗
        private async void ShowSimpleAlert(string msg)
        {
            ContentDialog d = new ContentDialog()
            {
                XamlRoot = this.XamlRoot,
                Title = "提示",
                Content = msg,
                CloseButtonText = "好"
            };
            await d.ShowAsync();
        }

        // 辅助方法：错误提示（简单版，WinUI 弹窗关闭后无法阻止关闭，所以这里仅做占位说明）
        private void ShowErrorTip(TextBox box, string msg)
        {
            box.Header = $"章节序号 - {msg}"; // 把错误显示在标题上提醒用户
            // 实际开发中通常会使用 MVVM 绑定错误信息，或者不关闭 Dialog
        }
    }
}