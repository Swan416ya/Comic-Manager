using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage.Pickers; // 文件选择器

namespace Comic_Manager
{
    public sealed partial class ShelfPage : Page
    {
        // 当前页面正在展示的分类ID（例如 "All" 或 "热血漫"）
        private string _currentCategory = "All";

        // 页面显示的数据源
        private ObservableCollection<ComicSeries> _displayedComics = new ObservableCollection<ComicSeries>();

        public ShelfPage()
        {
            this.InitializeComponent();
            // 绑定数据源
            ComicGridView.ItemsSource = _displayedComics;
        }

        // 1. 当导航到这个页面时触发
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // 获取传过来的分类名称
            if (e.Parameter is string categoryName)
            {
                _currentCategory = categoryName;
            }

            RefreshComicList();
        }

        // 刷新显示的漫画列表
        private void RefreshComicList()
        {
            _displayedComics.Clear();

            // 从全局数据中心筛选
            foreach (var comic in AppRepository.AllComics)
            {
                if (comic.Categories.Contains(_currentCategory))
                {
                    _displayedComics.Add(comic);
                }
            }
        }

        // 2. 点击右下角添加按钮
        private async void OnAddComicClick(object sender, RoutedEventArgs e)
        {
            // 复用下面的修改弹窗，只不过传个空对象进去
            await ShowEditComicDialog(null);
        }

        // 3. 右键菜单：修改信息
        private async void OnModifyClick(object sender, RoutedEventArgs e)
        {
            var menu = sender as MenuFlyoutItem;
            // 从 Tag 获取绑定的数据对象
            var comic = menu?.Tag as ComicSeries;
            if (comic != null)
            {
                await ShowEditComicDialog(comic);
            }
        }

        // 核心逻辑：添加/修改 弹窗
        private async Task ShowEditComicDialog(ComicSeries existingComic)
        {
            bool isNew = (existingComic == null);
            ComicSeries currentComic = existingComic ?? new ComicSeries();

            // 构建弹窗界面
            StackPanel content = new StackPanel() { Spacing = 12 };

            TextBox titleBox = new TextBox() { Header = "漫画标题", Text = currentComic.Title ?? "" };
            TextBox authorBox = new TextBox() { Header = "作者", Text = currentComic.Author ?? "" };

            // 封面选择区域
            TextBlock pathText = new TextBlock() { Text = currentComic.CoverPath ?? "未选择封面", Opacity = 0.6, FontSize = 12 };
            Button pickImgBtn = new Button() { Content = "选择封面图片" };
            pickImgBtn.Click += async (s, e) =>
            {
                // 打开文件选择器
                var picker = new FileOpenPicker();
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow); // 获取窗口句柄
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

                picker.FileTypeFilter.Add(".jpg");
                picker.FileTypeFilter.Add(".png");
                picker.FileTypeFilter.Add(".jpeg");

                var file = await picker.PickSingleFileAsync();
                if (file != null)
                {
                    pathText.Text = file.Path;
                    // 暂时把路径存在 Tag 里传出去
                    pathText.Tag = file.Path;
                }
            };

            content.Children.Add(titleBox);
            content.Children.Add(authorBox);
            content.Children.Add(pickImgBtn);
            content.Children.Add(pathText);

            ContentDialog dialog = new ContentDialog()
            {
                XamlRoot = this.XamlRoot,
                Title = isNew ? "添加新漫画" : "修改漫画信息",
                PrimaryButtonText = "保存",
                CloseButtonText = "取消",
                DefaultButton = ContentDialogButton.Primary,
                Content = content
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                // 更新对象数据
                currentComic.Title = titleBox.Text;
                currentComic.Author = authorBox.Text;

                // 如果选了新图片
                if (pathText.Tag != null)
                {
                    currentComic.CoverPath = pathText.Tag.ToString();
                }

                // 如果是新增，还要加到全局列表和当前分类
                if (isNew)
                {
                    // 如果当前不在 "All" 分类下，也要自动把新漫画加到当前分类
                    if (_currentCategory != "All" && !currentComic.Categories.Contains(_currentCategory))
                    {
                        currentComic.Categories.Add(_currentCategory);
                    }

                    AppRepository.AllComics.Add(currentComic);
                    // 刷新界面
                    RefreshComicList();
                }
            }
        }

        // 4. 右键菜单打开前的逻辑 (动态生成分类选项)
        private void OnContextMenuOpening(object sender, object e)
        {
            var flyout = sender as MenuFlyout;
            // 找到我们在 XAML 里定义的子菜单
            var addToMenu = flyout.Items.OfType<MenuFlyoutSubItem>().FirstOrDefault(i => i.Name == "AddToMenu");
            var removeFromMenu = flyout.Items.OfType<MenuFlyoutSubItem>().FirstOrDefault(i => i.Name == "RemoveFromMenu");

            // 也就是当前被右键点击的那个 GridViewItem 对应的数据
            // 这里稍微有点绕，因为 sender 是 MenuFlyout，我们需要找到它的 Target（Grid），然后获取 DataContext
            // 但 WinUI 的 MenuFlyout 没有简单的 Target 属性，我们换个思路：
            // 我们在 XAML 的 OnModifyClick 的 Tag 里绑定了 ComicSeries，但这里我们需要在 Opening 时获取。
            // 简单的做法是：让 GridView 的 SelectedItem 变成当前右键的项，但右键默认不选中。
            // 为了简化，我们假设用户必须先左键选中再右键（暂且这样，如果要完美右键需更复杂代码）。
            // 
            // *修正方案*：我们在 ItemTemplate 的 Grid 上直接处理 RightTapped 可能更准，但 Flyout 最简单。
            // 实际上，MenuFlyout.Target 属性在 WinUI 3 里还是很难用。
            // *最简单的黑科技*：把 ComicSeries 数据直接存在 Flyout 的 DataContext 里？不行。

            // 我们采用最稳妥的办法：遍历 Items，看看谁的 Flyout 是当前这个 sender
            // (这对初学者稍微有点超纲，但这是实现右键菜单动态化的必经之路)
            // 为了不卡在这里，我们换个简单逻辑：假设 GridView.SelectedItem 就是当前操作对象。
            // *注意*：使用时请先左键点一下选中，再右键。

            var targetComic = ComicGridView.SelectedItem as ComicSeries;
            if (targetComic == null) return; // 如果没选中，就不处理

            // --- 生成 "添加到" 列表 ---
            addToMenu.Items.Clear();
            foreach (var cat in AppRepository.AllCategories)
            {
                if (cat == "All") continue; // 不能添加到 All (默认就有)
                if (targetComic.Categories.Contains(cat)) continue; // 已经有了就不显示

                var item = new MenuFlyoutItem() { Text = cat };
                item.Click += (s, args) =>
                {
                    targetComic.Categories.Add(cat);
                    // 只有当我们在 "All" 视图时，不需要刷新；
                    // 但如果是其他视图，通常也不需要刷新，因为添加到了别的分类
                };
                addToMenu.Items.Add(item);
            }

            // --- 生成 "移出" 列表 ---
            removeFromMenu.Items.Clear();
            foreach (var cat in targetComic.Categories)
            {
                if (cat == "All") continue; // 不能移出 All

                var item = new MenuFlyoutItem() { Text = cat };
                item.Click += (s, args) =>
                {
                    targetComic.Categories.Remove(cat);
                    // 如果移出的正好是当前页面显示的分类，那它应该从眼前消失
                    if (_currentCategory == cat)
                    {
                        RefreshComicList();
                    }
                };
                removeFromMenu.Items.Add(item);
            }

            // 如果列表为空，禁用按钮
            addToMenu.IsEnabled = addToMenu.Items.Count > 0;
            removeFromMenu.IsEnabled = removeFromMenu.Items.Count > 0;
        }
        private void OnComicItemClick(object sender, ItemClickEventArgs e)
        {
            // 获取点击的漫画对象
            if (e.ClickedItem is ComicSeries clickedComic)
            {
                this.Frame.Navigate(typeof(DetailPage), clickedComic);
            }
        }
    }
}