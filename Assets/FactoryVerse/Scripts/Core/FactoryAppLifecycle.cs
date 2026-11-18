using UnityEngine;

namespace FactoryVerse
{
    /// <summary>
    /// Clean pause/resume for Quest: when the headset is taken off or the Meta system menu opens (app loses focus or is
    /// paused), game time and all audio stop; they resume exactly where they were. Created automatically in every scene.
    ///
    /// Freeze guard: on Quest the "focus regained" callback is not always delivered (hand-tracking prompts, the palm
    /// system gesture, notification pop-ups). Waiting for it left Time.timeScale at 0, so the story, the grab glide and
    /// the install animation stopped while head tracking kept running. Focus and pause are now tracked separately and
    /// re-checked every frame against Application.isFocused, so the game always resumes once the app is in front again.
    /// </summary>
    public class FactoryAppLifecycle : MonoBehaviour
    {
        static FactoryAppLifecycle _instance;
        bool _appPaused, _focusLost, _paused;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Create()
        {
            if (_instance) return;
            var go = new GameObject("App Lifecycle (pause/resume)");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<FactoryAppLifecycle>();
        }

        void OnApplicationFocus(bool focus) { if (!Application.isEditor) { _focusLost = !focus; Apply(); } }
        void OnApplicationPause(bool pause) { _appPaused = pause; if (!pause) _focusLost = !Application.isFocused && !Application.isEditor; Apply(); }

        void Update()   // still runs while timeScale is 0
        {
            if (_focusLost && Application.isFocused) { _focusLost = false; Apply(); }
            // Nothing in FactoryVerse pauses time on purpose, so a 0 time scale while we are not paused is always a bug.
            if (!_paused && Time.timeScale == 0f) Time.timeScale = 1f;
        }

        void Apply()
        {
            bool pause = _appPaused || _focusLost;
            if (pause == _paused) return;
            _paused = pause;
            Time.timeScale = pause ? 0f : 1f;
            AudioListener.pause = pause;
        }
    }
}
