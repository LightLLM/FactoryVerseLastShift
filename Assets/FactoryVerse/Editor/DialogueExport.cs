#if UNITY_EDITOR
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace FactoryVerse
{
    /// <summary>
    /// Exports the voice script as a CSV for the voice-generation agent: one row per line with the file name to produce,
    /// speaker, mission, priority, delivery direction, voice treatment, target speaking rate and text.
    /// Generated WAVs go in Assets/FactoryVerse/Resources/Dialogue/ under exactly those names; DialogueManager picks them up.
    /// Run: FactoryVerse > Dialogue > Export Voice Script (CSV).
    /// </summary>
    public static class DialogueExport
    {
        const string CsvPath = "Assets/FactoryVerse/Audio/FactoryVerse_VoiceScript.csv";
        const string ClipFolder = "Assets/FactoryVerse/Resources/Dialogue";

        [MenuItem("FactoryVerse/Dialogue/Export Voice Script (CSV)")]
        public static void Export()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(CsvPath));
            Directory.CreateDirectory(ClipFolder);
            var sb = new StringBuilder("file,speaker,mission,priority,direction,voice_treatment,words_per_minute,text\n");
            foreach (var l in DialogueScript.Lines)
                sb.AppendLine(string.Join(",", l.FileName, l.Speaker, l.Mission, l.Priority, Q(l.Direction), Q(Treatment(l.Speaker)),
                                          DialogueScript.WordsPerMinute(l.Speaker).ToString("0"), Q(l.Text)));
            File.WriteAllText(CsvPath, sb.ToString(), new UTF8Encoding(true));   // BOM so Excel reads the "·" and dashes
            File.WriteAllText(Path.Combine(ClipFolder, "README.txt"),
                "Put generated voice clips here, named exactly as in Assets/FactoryVerse/Audio/FactoryVerse_VoiceScript.csv (e.g. ARIA_M01_001.wav).\n" +
                "DialogueManager loads them by line id; lines without a clip still show subtitles.\n");
            AssetDatabase.Refresh();
            Debug.Log($"FactoryVerse: exported {DialogueScript.Lines.Length} dialogue lines to {CsvPath}; voice clips go in {ClipFolder}.");
        }

        static string Treatment(Speaker s) =>
            s == Speaker.ARIA ? "Feminine; calm, precise, slightly synthetic; clean synthetic, minimal reverb" :
            s == Speaker.Hale ? "Feminine; authoritative, grounded; light radio compression and static" :
            s == Speaker.R7 ? "Masculine or androgynous; low, synthetic, concise; subtle vocoder/electronic processing" :
                              "Masculine; natural, focused, restrained; close-mic";

        static string Q(string s) => "\"" + s.Replace("\"", "\"\"") + "\"";
    }
}
#endif
