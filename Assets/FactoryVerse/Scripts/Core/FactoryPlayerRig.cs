using UnityEngine;

namespace FactoryVerse
{
    /// <summary>
    /// Standalone first-person rig for exploring the FV-01 factory interior (FactoryInterior scene).
    /// Independent of MissionDirector/PlayerRig used by the LastShift game loop.
    ///
    /// Desktop controls:
    ///   WASD / arrows  move          Mouse        look (click the Game view to capture the cursor; Esc releases)
    ///   Shift          run           Space / Ctrl up / down while in Zero-G (jump when walking)
    ///   X              brake (Zero-G) L            headlamp
    ///   Tab            teleport to the next zone   1 / 2 / 3    lighting: Normal / Emergency / Zero-G
    /// Zero-G movement switches on automatically when the lighting preset is Zero-G.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class FactoryPlayerRig : MonoBehaviour
    {
        public Camera view;
        public Light headlamp;
        public FactoryLightingPresets lighting;
        public Transform[] teleportPoints;

        public float walkSpeed = 3.2f, runSpeed = 5.5f, jumpSpeed = 4f, gravity = 9.8f;
        public float zeroGSpeed = 2.7f, zeroGAccel = 3f, lookSensitivity = 2f;

        CharacterController _motor;
        Vector3 _velocity;
        float _yaw, _pitch;
        int _teleport = -1;
        string _notice = ""; float _noticeUntil;

        bool ZeroG => lighting && lighting.preset == FactoryLightingPresets.Preset.ZeroGravity;

        void Start()
        {
            _motor = GetComponent<CharacterController>();
            _yaw = transform.eulerAngles.y;
            if (headlamp) headlamp.enabled = false;
            Notice("Click to look around  •  WASD move  •  Tab next zone  •  1/2/3 lighting  •  L headlamp");
        }

        void Update()
        {
            // cursor capture
            if (FactoryVerse.FVInput.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
            if (FactoryVerse.FVInput.GetKeyDown(KeyCode.Escape)) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }

            if (Cursor.lockState == CursorLockMode.Locked)
            {
                _yaw += FactoryVerse.FVInput.GetAxis("Mouse X") * lookSensitivity;
                _pitch = Mathf.Clamp(_pitch - FactoryVerse.FVInput.GetAxis("Mouse Y") * lookSensitivity, -85f, 85f);
            }
            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            if (view) view.transform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);

            if (FactoryVerse.FVInput.GetKeyDown(KeyCode.L) && headlamp) headlamp.enabled = !headlamp.enabled;
            if (FactoryVerse.FVInput.GetKeyDown(KeyCode.Tab)) NextZone();

            var input = Vector3.ClampMagnitude(new Vector3(FactoryVerse.FVInput.GetAxisRaw("Horizontal"), 0f, FactoryVerse.FVInput.GetAxisRaw("Vertical")), 1f);
            if (ZeroG)
            {
                // drift in the look direction, thrust up/down, brake with X
                var wish = (view ? view.transform : transform).TransformDirection(input);
                wish += Vector3.up * ((FactoryVerse.FVInput.GetKey(KeyCode.Space) ? 1f : 0f) - (FactoryVerse.FVInput.GetKey(KeyCode.LeftControl) ? 1f : 0f));
                _velocity = Vector3.MoveTowards(_velocity, Vector3.ClampMagnitude(wish, 1f) * zeroGSpeed, zeroGAccel * Time.deltaTime);
                if (FactoryVerse.FVInput.GetKey(KeyCode.X)) _velocity = Vector3.MoveTowards(_velocity, Vector3.zero, 12f * Time.deltaTime);
            }
            else
            {
                float speed = FactoryVerse.FVInput.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;
                var move = transform.TransformDirection(input) * speed;
                _velocity.x = move.x; _velocity.z = move.z;
                if (_motor.isGrounded)
                {
                    _velocity.y = -1f;
                    if (FactoryVerse.FVInput.GetKeyDown(KeyCode.Space)) _velocity.y = jumpSpeed;
                }
                else _velocity.y -= gravity * Time.deltaTime;
            }
            _motor.Move(_velocity * Time.deltaTime);
        }

        void NextZone()
        {
            if (teleportPoints == null || teleportPoints.Length == 0) return;
            _teleport = (_teleport + 1) % teleportPoints.Length;
            var t = teleportPoints[_teleport];
            _motor.enabled = false;               // CharacterController overrides direct position changes
            transform.position = t.position;
            _motor.enabled = true;
            _yaw = t.eulerAngles.y; _pitch = 0f; _velocity = Vector3.zero;
            Notice(t.name);
        }

        void Notice(string text) { _notice = text; _noticeUntil = Time.time + 4f; }

        void OnGUI()
        {
            // crosshair
            float cx = Screen.width / 2f, cy = Screen.height / 2f;
            GUI.color = new Color(0.6f, 0.95f, 1f, 0.85f);
            GUI.DrawTexture(new Rect(cx - 2, cy - 2, 4, 4), Texture2D.whiteTexture);
            GUI.color = Color.white;

            var style = new GUIStyle(GUI.skin.label) { fontSize = Mathf.Max(12, Screen.height / 45) };
            string mode = ZeroG ? "ZERO-G (Space/Ctrl up/down, X brake)" : "WALK";
            string light = lighting ? lighting.preset.ToString() : "-";
            GUI.Label(new Rect(12, 8, Screen.width, 30), $"FV-01  •  {mode}  •  Lighting: {light}", style);
            if (Time.time < _noticeUntil)
                GUI.Label(new Rect(12, 8 + style.fontSize * 1.6f, Screen.width, 30), _notice, style);
        }
    }
}
