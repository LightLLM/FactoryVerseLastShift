using System.Collections.Generic;

namespace FactoryVerse
{
    public enum Speaker { ARIA, Hale, R7, Engineer }

    /// <summary>Critical lines jump the queue and cut off optional ones; optional lines are dropped rather than wait.</summary>
    public enum DialoguePriority { Optional = 0, Normal = 1, Critical = 2 }

    public sealed class DialogueLine
    {
        public readonly string Id, Mission, Direction, Text;
        public readonly Speaker Speaker;
        public readonly DialoguePriority Priority;
        public DialogueLine(string id, Speaker speaker, string mission, DialoguePriority priority, string direction, string text)
        { Id = id; Speaker = speaker; Mission = mission; Priority = priority; Direction = direction; Text = text; }

        /// <summary>Voice file name the voice agent should export (WAV), and Resources path the manager loads it from.</summary>
        public string FileName => Id + ".wav";
        public string ResourcePath => "Dialogue/" + Id;
    }

    /// <summary>
    /// FactoryVerse: Last Shift — complete voice dialogue script (six missions + gameplay barks), as data.
    /// Ids follow SPEAKER_MISSION_NNN (e.g. ARIA_M01_001); drop the matching clip into Resources/Dialogue/ to voice a line.
    /// </summary>
    public static class DialogueScript
    {
        const DialoguePriority C = DialoguePriority.Critical, N = DialoguePriority.Normal, O = DialoguePriority.Optional;

        public static readonly DialogueLine[] Lines =
        {
            // Mission 01 — The Last Shift (0:00–1:00, normal operations)
            L("ARIA_M01_001", Speaker.ARIA, "M01", C, "Opening · calm, welcoming", "Welcome aboard FactoryVerse Station FV-01. Orbital cycle two thousand, one hundred and eighty-four. Your final shift begins now."),
            L("HALE_M01_001", Speaker.Hale, "M01", N, "Radio · professional", "Engineer, this is Commander Hale. Complete the R-7 assembly, shut down the production line, and you're cleared to return home."),
            L("ENG_M01_001", Speaker.Engineer, "M01", O, "Optional · relaxed", "One last robot. Then I'm going home."),
            L("ARIA_M01_002", Speaker.ARIA, "M01", C, "Instruction", "Proceed to Assembly Bay A. Activate the workstation to begin."),
            L("ARIA_M01_003", Speaker.ARIA, "M01", C, "Workstation activated", "Workstation online. Three components remain before Unit R-7 is operational."),

            // Mission 02 — Unit R-7 (1:00–3:30, hands-on assembly)
            L("ARIA_M02_001", Speaker.ARIA, "M02", C, "Power core objective", "Locate the power core. Align it with the illuminated chest socket."),
            L("ARIA_M02_002", Speaker.ARIA, "M02", C, "Successful installation", "Power core secured. Energy transfer stable."),
            L("ARIA_M02_003", Speaker.ARIA, "M02", C, "Arm objective", "Next, install the right arm module. Rotate the connector until the alignment markers match."),
            L("ARIA_M02_004", Speaker.ARIA, "M02", C, "Successful installation", "Arm module locked. Motor calibration complete."),
            L("ARIA_M02_005", Speaker.ARIA, "M02", C, "Head objective", "Install the head unit to complete the assembly."),
            L("ARIA_M02_006", Speaker.ARIA, "M02", C, "All components installed", "Assembly verified. Initiating Unit R-7 startup sequence."),
            L("R7_M02_001", Speaker.R7, "M02", N, "First activation · mechanical, curious", "Unit R-7 online. Systems nominal. Awaiting assignment."),
            L("ENG_M02_001", Speaker.Engineer, "M02", O, "Optional · amused", "Welcome to the night shift."),
            L("R7_M02_002", Speaker.R7, "M02", N, "Dry humour", "Acknowledged. I was not informed that nights existed in orbit."),
            L("HALE_M02_001", Speaker.Hale, "M02", N, "Radio", "Excellent work. Secure the bay and prepare for departure."),

            // Mission 03 — Catastrophic Failure (3:30–4:30, emergency lighting)
            L("ARIA_M03_001", Speaker.ARIA, "M03", C, "Immediate emergency", "Warning. Critical overload detected in the primary power bus."),
            L("HALE_M03_001", Speaker.Hale, "M03", N, "Radio · urgent", "Engineering, report! We're losing power across the entire station!"),
            L("ENG_M03_001", Speaker.Engineer, "M03", O, "Optional · alarmed", "What just happened?"),
            L("ARIA_M03_002", Speaker.ARIA, "M03", C, "System report", "Primary power offline. Artificial gravity unstable. Life support operating on emergency reserves."),
            L("R7_M03_001", Speaker.R7, "M03", N, "Analytical", "Station failure probability increasing. Immediate intervention recommended."),
            L("HALE_M03_002", Speaker.Hale, "M03", N, "Radio", "Get to the emergency console. Find out what we can still save."),
            L("ARIA_M03_003", Speaker.ARIA, "M03", C, "Objective", "Activate the emergency maintenance console to isolate the damaged power bus."),

            // Mission 04 — Zero-Gravity Survival (4:30–7:30)
            L("ARIA_M04_001", Speaker.ARIA, "M04", C, "Emergency", "Artificial gravity offline. Secure loose equipment. Avoid uncontrolled movement."),
            L("ENG_M04_001", Speaker.Engineer, "M04", O, "Optional", "That's not good."),
            L("R7_M04_001", Speaker.R7, "M04", N, "Detection", "Replacement power cell located. Maintenance corridor, section four."),
            L("ARIA_M04_002", Speaker.ARIA, "M04", C, "Objective", "Recover the replacement power cell and insert it into the emergency power socket."),
            L("HALE_M04_001", Speaker.Hale, "M04", N, "Radio · strained", "Engineer, the station's coming apart. You need to move!"),
            L("ARIA_M04_003", Speaker.ARIA, "M04", C, "Low oxygen reminder", "Warning. Oxygen reserves below fifty percent."),
            L("R7_M04_002", Speaker.R7, "M04", N, "Guidance", "The power cell is drifting toward the ceiling. I recommend retrieving it now."),
            L("ENG_M04_002", Speaker.Engineer, "M04", O, "Optional · determined", "Got it. Heading for the power socket."),
            L("ARIA_M04_004", Speaker.ARIA, "M04", C, "Successful repair", "Emergency power restored. Escape pod systems responding."),
            L("R7_M04_003", Speaker.R7, "M04", N, "Quiet relief", "We have a path to survival."),

            // Mission 05 — Escape Protocol (7:30–10:00, orange emergency lighting)
            L("ARIA_M05_001", Speaker.ARIA, "M05", C, "Objective", "Proceed to the escape bay. Pod access remains locked pending power activation."),
            L("R7_M05_001", Speaker.R7, "M05", N, "Unlocking", "Access override accepted. Escape bay unlocked."),
            L("HALE_M05_001", Speaker.Hale, "M05", N, "Radio", "You're almost there. Don't stop now."),
            L("ARIA_M05_002", Speaker.ARIA, "M05", C, "Pod status", "Escape pod power at thirty-eight percent. Manual launch authorization required."),
            L("ENG_M05_001", Speaker.Engineer, "M05", O, "Optional", "Come on. Just one more system."),
            L("ARIA_M05_003", Speaker.ARIA, "M05", C, "Pod activated", "Pod power stabilized. Launch controls enabled."),
            L("R7_M05_002", Speaker.R7, "M05", N, "Final instruction", "Enter the pod. I will maintain the launch connection."),

            // Mission 06 — The Final Launch (10:00–12:00)
            L("HALE_M06_001", Speaker.Hale, "M06", N, "Radio · desperate", "Launch now! The station's structural supports are failing!"),
            L("ENG_M06_001", Speaker.Engineer, "M06", O, "Optional · determined", "Launching!"),
            L("ARIA_M06_001", Speaker.ARIA, "M06", C, "Countdown · steady", "Launch sequence initiated. Five. Four. Three. Two. One."),
            L("R7_M06_001", Speaker.R7, "M06", N, "Emotional but restrained", "Mission complete, Engineer. It was an honour to serve."),
            L("ARIA_M06_002", Speaker.ARIA, "M06", C, "Final line", "Escape confirmed. You survived the last shift."),
            L("HALE_M06_002", Speaker.Hale, "M06", N, "Radio · relieved", "We have your signal. Welcome home, Engineer."),

            // Gameplay barks — trigger only when needed
            L("ARIA_GEN_001", Speaker.ARIA, "GEN", C, "Wrong robot component", "Incorrect component. Check the alignment indicator."),
            L("ARIA_GEN_002", Speaker.ARIA, "GEN", N, "Player waits too long", "Engineer, your current objective remains incomplete."),
            L("R7_GEN_001", Speaker.R7, "GEN", N, "Robot part dropped", "Component displaced. Please recover it."),
            L("ARIA_GEN_003", Speaker.ARIA, "GEN", C, "Oxygen 25%", "Critical warning. Oxygen reserves below twenty-five percent."),
            L("ARIA_GEN_004", Speaker.ARIA, "GEN", C, "Pod not powered", "Launch unavailable. Restore pod power first."),
            L("ARIA_GEN_005", Speaker.ARIA, "GEN", C, "Mission failed", "Life support depleted. Mission terminated."),
            L("ARIA_GEN_006", Speaker.ARIA, "GEN", C, "Restart", "Simulation reset. Beginning a new shift."),
        };

        /// <summary>Story beats in script order, for playing a mission's lines back to back.</summary>
        public static readonly Dictionary<string, string[]> Sequences = new Dictionary<string, string[]>
        {
            { "M01_Intro",       new[] { "ARIA_M01_001", "HALE_M01_001", "ENG_M01_001", "ARIA_M01_002" } },
            { "M02_R7Online",    new[] { "ARIA_M02_006", "R7_M02_001", "ENG_M02_001", "R7_M02_002", "HALE_M02_001" } },
            { "M03_Failure",     new[] { "ARIA_M03_001", "HALE_M03_001", "ENG_M03_001", "ARIA_M03_002", "R7_M03_001", "HALE_M03_002", "ARIA_M03_003" } },
            { "M04_ZeroG",       new[] { "ARIA_M04_001", "ENG_M04_001", "R7_M04_001", "ARIA_M04_002", "HALE_M04_001" } },
            { "M04_Restored",    new[] { "ARIA_M04_004", "R7_M04_003" } },
            { "M05_Escape",      new[] { "ARIA_M05_001", "R7_M05_001", "HALE_M05_001", "ARIA_M05_002" } },
            { "M06_Launch",      new[] { "HALE_M06_001", "ENG_M06_001", "ARIA_M06_001", "R7_M06_001", "ARIA_M06_002", "HALE_M06_002" } },
        };

        /// <summary>Delivery speed per character (words per minute, mid-range of the voice direction) — used to time subtitles when no audio clip exists yet.</summary>
        public static float WordsPerMinute(Speaker s) => s == Speaker.ARIA ? 142f : s == Speaker.Hale ? 160f : s == Speaker.R7 ? 128f : 150f;

        static Dictionary<string, DialogueLine> _byId;
        public static DialogueLine Get(string id)
        {
            if (_byId == null) { _byId = new Dictionary<string, DialogueLine>(); foreach (var l in Lines) _byId[l.Id] = l; }
            return _byId.TryGetValue(id, out var line) ? line : null;
        }

        static DialogueLine L(string id, Speaker s, string mission, DialoguePriority p, string direction, string text) => new DialogueLine(id, s, mission, p, direction, text);
    }
}
