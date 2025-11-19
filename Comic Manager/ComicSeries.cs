using Microsoft.UI.Xaml.Media.Imaging; // 用于图片
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel; // 用于数据更新通知

namespace Comic_Manager
{
    // 继承 INotifyPropertyChanged 是为了让数据变了界面能自动刷新
    public class ComicSeries : INotifyPropertyChanged
    {
        public string Id { get; set; } = Guid.NewGuid().ToString(); // 唯一ID

        private string _title;
        public string Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(nameof(Title)); }
        }

        private string _author;
        public string Author
        {
            get => _author;
            set { _author = value; OnPropertyChanged(nameof(Author)); }
        }

        private string _coverPath;
        public string CoverPath
        {
            get => _coverPath;
            set
            {
                _coverPath = value;
                OnPropertyChanged(nameof(CoverPath));
                // 当路径改变时，通知封面图片也更新
                OnPropertyChanged(nameof(CoverImageBitmap));
            }
        }

        // 用于绑定的图片对象
        public BitmapImage CoverImageBitmap
        {
            get
            {
                if (string.IsNullOrEmpty(CoverPath)) return null;
                try { return new BitmapImage(new Uri(CoverPath)); }
                catch { return null; }
            }
        }

        // 这本漫画属于哪些分类（比如 "热血", "悬疑"）
        // "All" 默认大家都属于
        public List<string> Categories { get; set; } = new List<string>() { "All" };

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public ObservableCollection<ComicChapter> Chapters { get; set; } = new ObservableCollection<ComicChapter>();
    }
}