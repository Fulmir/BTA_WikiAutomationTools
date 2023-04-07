using System.Text.RegularExpressions;

namespace BT_JsonProcessingLibrary
{
    public static class ModJsonHandler
    {

        public static List<BasicFileData> SearchFiles(string startingPath, string filePattern)
        {
            List<BasicFileData> values = new List<BasicFileData>();

            string? dirName = Path.GetDirectoryName(startingPath);
            string? fileName = Path.GetFileName(startingPath);

            var files = from file in Directory.EnumerateFiles(
                            string.IsNullOrWhiteSpace(dirName) ? "." : dirName,
                            filePattern,
                            SearchOption.AllDirectories)
                        select file;

            foreach (var file in files)
            {
                values.Add(new BasicFileData { Path = Path.GetFullPath(file), FileName = Path.GetFileName(file) });
            }

            return values;
        }
    }
}