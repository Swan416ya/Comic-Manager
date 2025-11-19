using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Comic_Manager
{
    public sealed partial class ReadingPage : Page
    {
        // 定义每一屏显示的数据结构
        public class PageItem
        {
            // 不管是日漫还是国漫，ImageLeft 就显示在屏幕左边，ImageRight 就显示在屏幕右边
            // 我们在 C# 里决定谁左谁右
            public string ImageLeft { get; set; }
            public string ImageRight { get; set; }

            // 辅助属性：用于计算页码显示
            public string PageNumberDisplay { get; set; }

            // 辅助属性：控制显示隐藏
            public Visibility ImageLeftVis => string.IsNullOrEmpty(ImageLeft) ? Visibility.Collapsed : Visibility.Visible;
            public Visibility ImageRightVis => string.IsNullOrEmpty(ImageRight) ? Visibility.Collapsed : Visibility.Visible;
        }

        // 全局变量，用于计算页码
        private ReadingMode _currentMode;
        private int _totalImageCount = 0;

        public ReadingPage()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is Tuple<ComicChapter, ReadingMode> param)
            {
                var chapter = param.Item1;
                _currentMode = param.Item2;

                await LoadImagesAsync(chapter.SourceFolderPath, _currentMode);
            }
        }

        private async Task LoadImagesAsync(string folderPath, ReadingMode mode)
        {
            if (!Directory.Exists(folderPath)) return;

            var extensions = new string[] { ".jpg", ".jpeg", ".png", ".bmp", ".webp" };
            var files = Directory.GetFiles(folderPath)
                                 .Where(f => extensions.Contains(Path.GetExtension(f).ToLower()))
                                 .ToList();

            // 自然排序
            files.Sort(new NaturalStringComparer());
            _totalImageCount = files.Count;

            if (mode == ReadingMode.Webtoon)
            {
                SetupWebtoonMode(files);
            }
            else
            {
                SetupPagedMode(files, mode);
            }
        }

        private void SetupWebtoonMode(List<string> files)
        {
            WebtoonViewer.Visibility = Visibility.Visible;
            PagedFlipView.Visibility = Visibility.Collapsed;
            WebtoonList.ItemsSource = files;

            // 条漫模式初始页码
            UpdateWebtoonIndicator();
        }

        private void SetupPagedMode(List<string> files, ReadingMode mode)
        {
            WebtoonViewer.Visibility = Visibility.Collapsed;
            PagedFlipView.Visibility = Visibility.Visible;

            var pageItems = new List<PageItem>();

            // === 1. 普通单页模式 ===
            if (mode == ReadingMode.SinglePage)
            {
                PagedFlipView.FlowDirection = FlowDirection.LeftToRight; // 正常习惯

                for (int i = 0; i < files.Count; i++)
                {
                    // 单页模式我们把图放在 Left 槽位，Right 留空
                    // 因为 Grid 是 50:50，为了居中，我们在 XAML 里用了 Center 对齐，
                    // 如果只有一张图，它会显示在左半边？
                    // 修正：为了单页居中，我们可以利用 XAML Grid 特性。
                    // 但这里最简单的 Hack 是：单页模式时，XAML 其实可以不需要两列。
                    // 不过为了共用 Template，我们把图放在 Left，但在 XAML Grid 里
                    // 实际上单页模式建议 Left 和 Right 设为 null，单页图单独处理？
                    // 为了不改 XAML 结构，我们将单页图放在 Left，
                    // 注意：为了让单页居中，最完美的做法是改 Template，
                    // 但这里我们让 Left 占满，把 Right 设空。

                    pageItems.Add(new PageItem
                    {
                        ImageLeft = files[i],
                        ImageRight = null,
                        PageNumberDisplay = (i + 1).ToString()
                    });
                }
            }
            // === 2. 国漫模式 (双页，从左往右) ===
            else if (mode == ReadingMode.ChinaManga)
            {
                PagedFlipView.FlowDirection = FlowDirection.LeftToRight;

                // 逻辑：Page 1 在左，Page 2 在右
                for (int i = 0; i < files.Count; i += 2)
                {
                    var item = new PageItem();
                    item.ImageLeft = files[i]; // 小页码在左

                    if (i + 1 < files.Count)
                    {
                        item.ImageRight = files[i + 1]; // 大页码在右
                        item.PageNumberDisplay = $"{i + 1} / {i + 2}";
                    }
                    else
                    {
                        item.ImageRight = null;
                        item.PageNumberDisplay = (i + 1).ToString();
                    }
                    pageItems.Add(item);
                }
            }
            // === 3. 日漫模式 (双页，从右往左) ===
            else if (mode == ReadingMode.Manga)
            {
                // 【关键】开启右向左导航 (点左=下一页，点右=上一页)
                PagedFlipView.FlowDirection = FlowDirection.RightToLeft;

                // 逻辑：拼图时，屏幕视觉上的左边应该是大页码，右边是小页码
                // 例如：[P2] [P1]
                // 因为我们 XAML 里的 Grid 并没有随 FlipView 翻转 (ItemTemplate 内部强制 L2R)
                // 所以 ImageLeft 对应屏幕左边，ImageRight 对应屏幕右边

                for (int i = 0; i < files.Count; i += 2)
                {
                    var item = new PageItem();

                    // 这里的 i 是 0 (Page 1)
                    // 我们希望 Page 1 出现在屏幕右边 (ImageRight)
                    item.ImageRight = files[i];

                    if (i + 1 < files.Count)
                    {
                        // Page 2 出现在屏幕左边 (ImageLeft)
                        item.ImageLeft = files[i + 1];
                        item.PageNumberDisplay = $"{i + 1} / {i + 2}";
                    }
                    else
                    {
                        item.ImageLeft = null; // 最后一页如果是单数，左边这就空着
                        item.PageNumberDisplay = (i + 1).ToString();
                    }
                    pageItems.Add(item);
                }
            }

            PagedFlipView.ItemsSource = pageItems;
            // 触发一次页码更新
            UpdatePageIndicator();
        }

        // 当翻页发生变化时
        private void OnPageSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdatePageIndicator();
        }

        // 更新右下角页码逻辑
        private void UpdatePageIndicator()
        {
            if (_currentMode == ReadingMode.Webtoon) return;

            if (PagedFlipView.SelectedItem is PageItem currentItem)
            {
                // 直接读取我们在创建 Item 时预生成的页码字符串
                PageIndicatorText.Text = currentItem.PageNumberDisplay;
            }
        }

        // 条漫滚动时更新 (可选，简单显示总页数)
        private void OnWebtoonViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            UpdateWebtoonIndicator();
        }

        private void UpdateWebtoonIndicator()
        {
            // 条漫很难精确计算当前看到第几张图，这里简单显示 "条漫模式 Total: XX"
            // 或者你可以计算 ScrollViewer.VerticalOffset / Height
            PageIndicatorText.Text = $"Total: {_totalImageCount}";
        }
    }
}