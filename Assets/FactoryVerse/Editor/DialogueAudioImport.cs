#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace FactoryVerse
{
    /// <summary>Voice clips in Resources/Dialogue import small for Quest: mono, Vorbis, decompress on load (short lines).</summary>
    class DialogueAudioImport : AssetPostprocessor
    {
        void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith("Assets/FactoryVerse/Resources/Dialogue/")) return;
            var imp = (AudioImporter)assetImporter;
            imp.forceToMono = true;
            imp.loadInBackground = true;
            var s = imp.defaultSampleSettings;
            s.loadType = AudioClipLoadType.DecompressOnLoad;
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            s.quality = 0.6f;
            s.sampleRateSetting = AudioSampleRateSetting.OptimizeSampleRate;
            imp.defaultSampleSettings = s;
        }
    }
}
#endif
