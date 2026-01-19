using System.IO;
using Path = System.IO.Path;

namespace WindowSill.ImageHelper;

internal static class ImageFileNameHelper
{
    internal static string GetVariantFilePath(string originalFilePath, string suffix)
    {
        string directory = Path.GetDirectoryName(originalFilePath)!;
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(originalFilePath);
        string extension = Path.GetExtension(originalFilePath);

        string candidate = Path.Combine(directory, $"{fileNameWithoutExtension}{suffix}{extension}");
        if (!File.Exists(candidate))
        {
            return candidate;
        }

        int index = 2;
        string numberedCandidate;
        do
        {
            numberedCandidate = Path.Combine(directory, $"{fileNameWithoutExtension}{suffix}{index}{extension}");
            index++;
        }
        while (File.Exists(numberedCandidate));

        return numberedCandidate;
    }
}
