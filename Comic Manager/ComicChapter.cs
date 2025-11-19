namespace Comic_Manager
{
    public class ComicChapter
    {
        public string Id { get; set; } = System.Guid.NewGuid().ToString();

        // 章节号 (存 double 是为了方便排序，比如 1, 1.5, 2)
        public double ChapterNumber { get; set; }

        // 显示的标题，比如 "第 1 话"
        public string DisplayTitle => $"第 {ChapterNumber} 话";

        // 图片所在的文件夹路径
        public string SourceFolderPath { get; set; }
    }
}