namespace RetailAppCore.Models
{
    public class FileModel
    {
        public string FileName { get; set; }
        public long FileSize { get; set; }
        public DateTimeOffset? LastModified { get; set; }

        public string DisplaySize
        {
            get
            {
                if(FileSize >= 1024 *1024)
                    return $"{FileSize / 1024 / 1024} MB";
                if(FileSize >= 1024)
                    return $"{FileSize / 1024} KB";
                return $"{FileSize} Bytes";
            }
        }
    }
}
