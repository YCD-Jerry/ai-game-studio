// Example shot: a slow push between two camera poses, every frame a pure function of its index.
// run_shot.py replaces EVERY (30 for a preview, 1 for the real take) and SUFFIX ("_p" for previews).
// Edit the two poses and the frame count; everything else comes from capture_lib.cs.
var every = EVERY; var suffix = "SUFFIX";
const int N = 360;                                   // 6 s at 60 fps
var e0 = new UnityEngine.Vector3(0f, 1.6f, -20f);    // eye at the start
var a0 = new UnityEngine.Vector3(0f, 1.2f, 0f);      // looking at
var e1 = new UnityEngine.Vector3(0.8f, 1.9f, -9f);
var a1 = new UnityEngine.Vector3(0f, 1.4f, 0f);

System.Collections.IEnumerator Run()
{
    UnityEngine.Time.captureFramerate = 60;
    for (int i = 0; i < 120; i++) { Halt(); yield return null; }   // let the scene settle with the story paused
    HudVisible(false);
    var cam = TakeCamera(38f);
    for (int f = 0; f < N; f++)
    {
        // Ease in, and end still moving a little: a shot never parks.
        float t = Ease(f / (float)(N - 1)) * 0.94f + f / (float)(N - 1) * 0.06f;
        cam.transform.position = UnityEngine.Vector3.Lerp(e0, e1, t);
        cam.transform.rotation = UnityEngine.Quaternion.LookRotation(UnityEngine.Vector3.Lerp(a0, a1, t) - cam.transform.position);
        yield return end;
        if (Keep(f, every)) Grab("example" + suffix, f);
    }
    HudVisible(true);
    Finish("example done, " + N + " frames");
}
host.StartCoroutine(Run());
return "started";
