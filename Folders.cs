using Colossal.PSI.Environment;
using System.IO;
using static System.Net.Mime.MediaTypeNames;

namespace test
{
    public static class Folders
    {
        public static string ContentFolder { get; }
        public static string SettingsFolder { get; }
        public static string TempFolder { get; }

        static Folders()
        {
            ContentFolder = Path.Combine(EnvPath.kUserDataPath, "ModsData", nameof(test));

            SettingsFolder = Path.Combine(EnvPath.kUserDataPath, "ModsSettings", nameof(test));

            TempFolder = Path.Combine(EnvPath.kTempDataPath, nameof(test));

            Directory.CreateDirectory(ContentFolder);   // Only create those
            Directory.CreateDirectory(SettingsFolder);  // folders if you
            Directory.CreateDirectory(TempFolder);      // need them
        }
    }
}