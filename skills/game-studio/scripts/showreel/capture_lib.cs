// Showreel capture library. run_shot.py pastes it ahead of a shot file and runs the result through the editor bridge
// (`unity command script eval_file`), so it is a method body: local functions only, nothing goes into Assets/.
// Needs Play mode, the Input System package, and a Game view fixed at 1920x1080 before Play starts.
// ROOT is replaced by run_shot.py with SHOWREEL_ROOT (forward slashes).
if (!UnityEditor.EditorApplication.isPlaying) return "not playing";
if (UnityEngine.Screen.width != 1920 || UnityEngine.Screen.height != 1080) return "wrong size " + UnityEngine.Screen.width + "x" + UnityEngine.Screen.height;

// Keys reach the game without window focus while the take runs (put back by Finish).
var inputSettings = UnityEngine.InputSystem.InputSystem.settings;
var bgWas = inputSettings.backgroundBehavior;
var edWas = inputSettings.editorInputBehaviorInPlayMode;
inputSettings.backgroundBehavior = UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
inputSettings.editorInputBehaviorInPlayMode = UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
var kb = UnityEngine.InputSystem.Keyboard.current;
UnityEngine.InputSystem.InputSystem.EnableDevice(kb);

// A host object runs the shot's coroutine and survives scene loads.
var host = new UnityEngine.GameObject("ShowreelHost").AddComponent<UnityEngine.UI.LayoutElement>();
UnityEngine.Object.DontDestroyOnLoad(host.gameObject);
var capRt = new UnityEngine.RenderTexture(1920, 1080, 0, UnityEngine.RenderTextureFormat.ARGB32);
var capTex = new UnityEngine.Texture2D(1920, 1080, UnityEngine.TextureFormat.RGB24, false);
var end = new UnityEngine.WaitForEndOfFrame();
var root = "ROOT/frames/";
var asyncWas = UnityEditor.EditorSettings.asyncShaderCompilation;
UnityEditor.EditorSettings.asyncShaderCompilation = false;   // no flat-cyan placeholders in the take

// Hold exactly these keys down from now on (call with nothing to release all).
void Keys(params UnityEngine.InputSystem.Key[] keys) =>
    UnityEngine.InputSystem.InputSystem.QueueStateEvent(kb, new UnityEngine.InputSystem.LowLevel.KeyboardState(keys));

// Save frame f of a shot. Call right after `yield return end`. The image comes out upside down; frames_to_clip.py flips it.
void Grab(string shot, int f)
{
    var dir = root + shot; System.IO.Directory.CreateDirectory(dir);
    UnityEngine.ScreenCapture.CaptureScreenshotIntoRenderTexture(capRt);
    var prev = UnityEngine.RenderTexture.active; UnityEngine.RenderTexture.active = capRt;
    capTex.ReadPixels(new UnityEngine.Rect(0, 0, 1920, 1080), 0, 0, false);
    UnityEngine.RenderTexture.active = prev;
    System.IO.File.WriteAllBytes(dir + "/" + f.ToString("D5") + ".png", UnityEngine.ImageConversion.EncodeToPNG(capTex));
}

// Pause the story: stop every enabled behaviour whose type name ends with "Director" (rename to suit your project),
// so no line, fade or camera move fires mid-shot. Call it every frame while waiting for a scene to settle.
void Halt()
{
    foreach (var b in UnityEngine.Object.FindObjectsByType<UnityEngine.MonoBehaviour>(UnityEngine.FindObjectsSortMode.None))
        if (b.enabled && b.GetType().Name.EndsWith("Director") && b.GetType().Name != "AudioDirector") { b.StopAllCoroutines(); b.enabled = false; }
}

// Show or hide every root canvas (the HUD, prompts, menus).
void HudVisible(bool on)
{
    foreach (var c in UnityEngine.Object.FindObjectsByType<UnityEngine.Canvas>(UnityEngine.FindObjectsSortMode.None))
        if (c.isRootCanvas) c.enabled = on;
}

// Take the main camera away from the camera rig for a scripted shot.
// With Cinemachine 3, disable its brain: cam.GetComponent<Unity.Cinemachine.CinemachineBrain>().enabled = false;
UnityEngine.Camera TakeCamera(float fov)
{
    var cam = UnityEngine.Camera.main;
    foreach (var mb in cam.GetComponents<UnityEngine.MonoBehaviour>())
        if (mb.GetType().Name == "CinemachineBrain") mb.enabled = false;
    cam.fieldOfView = fov;
    return cam;
}

// Move a character without its controller or agent fighting back.
void Place(UnityEngine.Transform who, UnityEngine.Vector3 at, float yaw)
{
    var c = who.GetComponent<UnityEngine.CharacterController>(); if (c != null) c.enabled = false;
    var a = who.GetComponent<UnityEngine.AI.NavMeshAgent>();
    if (a != null && a.enabled) a.Warp(at); else who.position = at;
    who.rotation = UnityEngine.Quaternion.Euler(0f, yaw, 0f);
    if (c != null) c.enabled = true;
}

// End of a take: release keys, put back what the capture changed, write the status file run_shot.py waits for.
void Finish(string note)
{
    Keys();
    UnityEngine.Time.captureFramerate = 0;
    UnityEditor.EditorSettings.asyncShaderCompilation = asyncWas;
    inputSettings.backgroundBehavior = bgWas;
    inputSettings.editorInputBehaviorInPlayMode = edWas;
    capRt.Release();
    System.IO.File.WriteAllText(root + "status.txt", note);
    UnityEngine.Object.Destroy(host.gameObject);
}

float Ease(float t) { t = UnityEngine.Mathf.Clamp01(t); return t * t * (3f - 2f * t); }
float Heading(UnityEngine.Vector3 v) => UnityEngine.Mathf.Atan2(v.x, v.z) * UnityEngine.Mathf.Rad2Deg;

// Preview keeps one frame in `every`; the real take keeps them all (every = 1).
bool Keep(int f, int every) => every <= 1 || f % every == 0;
