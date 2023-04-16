namespace UtilityClassLibrary
{
    public static class TextFileListProcessor
    {
        public static List<string> GetStringListFromFile(string relativeFilePath)
        {
            StreamReader textFileReader = new StreamReader(relativeFilePath);
            List<string> outputList = new List<string>();

            while (!textFileReader.EndOfStream)
                outputList.Add(textFileReader.ReadLine());

            textFileReader.Close();
            return outputList;
        }
    }
}