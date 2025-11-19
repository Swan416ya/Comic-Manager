using System.Collections.ObjectModel;

namespace Comic_Manager
{
    public static class AppRepository
    {
        // 存放所有漫画的列表
        // ObservableCollection 会自动通知 UI 增删变化
        public static ObservableCollection<ComicSeries> AllComics { get; set; } = new ObservableCollection<ComicSeries>();

        // 存放当前所有的分类名称（跟侧边栏同步）
        public static ObservableCollection<string> AllCategories { get; set; } = new ObservableCollection<string>() { "All" };
    }
}