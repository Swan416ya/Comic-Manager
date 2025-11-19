using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Comic_Manager
{
    public sealed partial class MainWindow : Window
    {
        // 数据结构：现在只需要存 Glyph (图标代码)
        // 虽然界面不显示名字，但建议保留 Name 字段方便你自己维护代码时知道这是啥
        private class IconOption
        {
            public string Name { get; set; } // 这个名字只在代码里看，界面上不显示
            public string Glyph { get; set; }
        }

        // === 预设图标库 ===
        // 你可以去 "WinUI 3 Gallery" 或微软官网找更多 "\uXXXX" 格式的代码加进去
        private readonly List<IconOption> _presetIcons = new List<IconOption>()
        {
            new IconOption { Name = "书本", Glyph = "\uE82D" },
            new IconOption { Name = "库", Glyph = "\uE8F1" },
            new IconOption { Name = "阅读", Glyph = "\uE736" },
            new IconOption { Name = "喜欢", Glyph = "\uEB51" }, // 爱心
            new IconOption { Name = "文件夹", Glyph = "\uE8B7" },
            new IconOption { Name = "图片", Glyph = "\uEB9F" },
            new IconOption { Name = "人物", Glyph = "\uE77B" },
            new IconOption { Name = "星球", Glyph = "\uE909" },
            new IconOption { Name = "皇冠", Glyph = "\uE734" }, // 星星/皇冠类
            new IconOption { Name = "闹钟", Glyph = "\uE916" }, // 追更
            new IconOption { Name = "日历", Glyph = "\uE787" },
            new IconOption { Name = "下载", Glyph = "\uE896" },
            new IconOption { Name = "链接", Glyph = "\uE71B" },
            new IconOption { Name = "锁", Glyph = "\uE72E" },   // 隐藏/加密
            new IconOption { Name = "标签", Glyph = "\uE8EC" },
            new IconOption { Name = "列表", Glyph = "\uEA37" },
        };

        public MainWindow()
        {
            this.InitializeComponent();

            this.Title = "Comic Manager";
            TrySetSystemBackdrop();

            // 沉浸式标题栏
            this.ExtendsContentIntoTitleBar = true;
            this.SetTitleBar(AppTitleBar);

            // 确保 AppWindow 静态引用可用 (为了 ShelfPage 里的 FilePicker)
            // 这是一种简单的 Hack，为了让 ShelfPage 能拿到 MainWindow 的句柄
            App.MainWindow = this;

            // 默认加载全部漫画页面
            ContentFrame.Navigate(typeof(ShelfPage), "All");
        }

        private async void MainNav_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            var clickedItem = args.InvokedItemContainer as NavigationViewItem;
            if (clickedItem != null)
            {
                string tag = clickedItem.Tag?.ToString();

                if (tag == "AddCategory") { await ShowAddCategoryDialog(); }
                else if (tag == "Settings") { ContentFrame.Navigate(typeof(SettingsPage)); }
                else if (tag == "All")
                {
                    // 跳转到书架页面，参数是 "All"
                    ContentFrame.Navigate(typeof(ShelfPage), "All");
                }
                else
                {
                    // 跳转到书架页面，参数是分类名
                    // 注意：这里假设 content 就是分类名
                    string categoryName = clickedItem.Content.ToString();
                    ContentFrame.Navigate(typeof(ShelfPage), categoryName);
                }
            }
        }

        // === 核心修改：纯图标选择弹窗 ===
        private async Task ShowAddCategoryDialog()
        {
            StackPanel dialogContent = new StackPanel() { Spacing = 16 };

            // 1. 输入框
            TextBox inputTextBox = new TextBox()
            {
                Header = "分类名称",
                PlaceholderText = "例如: 悬疑类"
            };

            // 2. 图标选择网格 (GridView) 代替下拉框
            // GridView 可以让图标横向排列，自动换行
            GridView iconGridView = new GridView();
            iconGridView.SelectionMode = ListViewSelectionMode.Single; // 单选
            iconGridView.Header = "选择图标";
            iconGridView.Height = 160; // 固定高度，内容多了可以滚动
            iconGridView.BorderThickness = new Thickness(1);
            iconGridView.BorderBrush = Application.Current.Resources["ControlElevationBorderBrush"] as Brush; // 给个边框好看点
            iconGridView.Padding = new Thickness(5);

            // 填充 GridView
            foreach (var iconOption in _presetIcons)
            {
                // 创建每一个图标项
                // GridViewItem 是容器，我们只往里面放一个 FontIcon
                GridViewItem item = new GridViewItem();

                // 设置每个格子的大小和边距
                item.Width = 45;
                item.Height = 45;
                item.Margin = new Thickness(2);

                // 只有图标，没有文字
                item.Content = new FontIcon()
                {
                    Glyph = iconOption.Glyph,
                    FontFamily = new FontFamily("Segoe Fluent Icons"),
                    FontSize = 20 // 图标大一点
                };

                // 仍然把 Glyph 藏在 Tag 里供后续读取
                item.Tag = iconOption.Glyph;

                // 添加提示工具 (Tooltip)，这样鼠标悬停时能看到 "书本" 这样的字
                ToolTipService.SetToolTip(item, iconOption.Name);

                iconGridView.Items.Add(item);
            }

            // 默认选中第一个
            if (iconGridView.Items.Count > 0)
            {
                iconGridView.SelectedIndex = 0;
            }

            // 将控件加入容器
            dialogContent.Children.Add(inputTextBox);
            dialogContent.Children.Add(iconGridView);

            // 3. 弹窗设置
            ContentDialog dialog = new ContentDialog()
            {
                XamlRoot = this.Content.XamlRoot,
                Title = "新建书架",
                PrimaryButtonText = "创建",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Primary,
                Content = dialogContent
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                string newCategoryName = inputTextBox.Text;

                // 获取选中的图标
                string selectedGlyph = _presetIcons[0].Glyph; // 默认值

                if (iconGridView.SelectedItem is GridViewItem selectedItem && selectedItem.Tag is string glyph)
                {
                    selectedGlyph = glyph;
                }

                if (!string.IsNullOrWhiteSpace(newCategoryName))
                {
                    AddNewCategoryToMenu(newCategoryName, selectedGlyph);
                }
            }
        }

        private void AddNewCategoryToMenu(string name, string glyph)
        {
            NavigationViewItem newItem = new NavigationViewItem();
            newItem.Content = name;
            newItem.Tag = name;

            newItem.Icon = new FontIcon()
            {
                Glyph = glyph,
                FontFamily = new FontFamily("Segoe Fluent Icons")
            };

            MainNav.MenuItems.Add(newItem);
            if (!AppRepository.AllCategories.Contains(name))
            {
                AppRepository.AllCategories.Add(name);
            }
            MainNav.SelectedItem = newItem;

            // 刷新右侧
            // 直接调用逻辑更新，比触发 ItemInvoked 更稳妥
            ContentFrame.Content = new TextBlock()
            {
                Text = $"分类: {name}",
                FontSize = 24,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        private void TrySetSystemBackdrop()
        {
            if (Microsoft.UI.Composition.SystemBackdrops.MicaController.IsSupported())
            {
                this.SystemBackdrop = new MicaBackdrop();
            }
        }
    }
}