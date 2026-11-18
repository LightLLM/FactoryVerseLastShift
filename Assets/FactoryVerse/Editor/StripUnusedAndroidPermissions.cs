using System.IO;
using System.Xml;
using UnityEditor.Android;
using UnityEngine;

namespace FactoryVerse
{
    /// <summary>
    /// FactoryVerse never records audio or talks to Bluetooth, but packages in the Meta XR template
    /// (Voice SDK / Microphone references) make Unity add those permissions, which shows a microphone
    /// prompt on launch and gets flagged in store review. Remove them from the generated manifest.
    /// </summary>
    class StripUnusedAndroidPermissions : IPostGenerateGradleAndroidProject
    {
        static readonly string[] Unused =
        {
            "android.permission.RECORD_AUDIO",
            "android.permission.MODIFY_AUDIO_SETTINGS",
            "android.permission.BLUETOOTH",
        };

        public int callbackOrder => 999; // after the Meta / OpenXR manifest processors

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            var manifestPath = Path.Combine(path, "src", "main", "AndroidManifest.xml");
            if (!File.Exists(manifestPath)) return;

            var doc = new XmlDocument();
            doc.Load(manifestPath);
            const string ns = "http://schemas.android.com/apk/res/android";
            int removed = 0;
            var nodes = new System.Collections.Generic.List<XmlElement>();
            foreach (XmlElement e in doc.SelectNodes("/manifest/uses-permission")) nodes.Add(e);
            foreach (var e in nodes)
            {
                var name = e.GetAttribute("name", ns);
                if (System.Array.IndexOf(Unused, name) >= 0) { e.ParentNode.RemoveChild(e); removed++; }
            }
            if (removed > 0)
            {
                doc.Save(manifestPath);
                Debug.Log($"FactoryVerse: removed {removed} unused Android permission(s) (microphone/Bluetooth) from the manifest.");
            }
        }
    }
}
