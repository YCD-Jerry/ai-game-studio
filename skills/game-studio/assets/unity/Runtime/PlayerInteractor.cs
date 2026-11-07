using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using Game.Core;

namespace Game.Gameplay
{
    /// <summary>Picks the best InteractPoint in reach and in front of the view, shows "[F] Open", handles press and hold on one key.</summary>
    public class PlayerInteractor : MonoBehaviour
    {
        public const string DefaultBinding = "<Keyboard>/f";   // project rule: interact is F (never E), in code, prompts and docs

        [SerializeField] string m_Binding = DefaultBinding;
        [SerializeField] Transform m_View;                     // the camera; defaults to Camera.main
        [SerializeField] TMP_Text m_Prompt;                    // optional HUD label

        InputAction m_Action;
        public InteractPoint Current { get; private set; }
        public float HoldProgress { get; private set; }
        public event Action<InteractPoint> FocusChanged;

        void Awake()
        {
            // A plain button: no Hold interaction on the action, so press and hold can share the key (read by hand below).
            m_Action = new InputAction("Interact", InputActionType.Button, m_Binding);
            if (m_View == null && Camera.main != null) m_View = Camera.main.transform;
        }

        void OnEnable() => m_Action.Enable();
        void OnDisable() { m_Action.Disable(); SetCurrent(null); }
        void OnDestroy() => m_Action?.Dispose();

        /// <summary>Rebind (settings page). Prompts re-read the display string, so they always show the player's key.</summary>
        public void Rebind(string controlPath) { m_Action.ApplyBindingOverride(0, controlPath); RefreshPrompt(); }
        public string KeyLabel => m_Action.GetBindingDisplayString(0);

        void Update()
        {
            var best = FindBest();
            if (best != Current) SetCurrent(best);
            if (Current == null || Time.timeScale == 0f) return;

            if (!Current.Hold)
            {
                if (m_Action.WasPressedThisFrame()) Current.Interact();
                return;
            }
            if (m_Action.IsPressed())
            {
                HoldProgress += Time.deltaTime / Mathf.Max(Current.HoldSeconds, 0.01f);
                if (HoldProgress >= 1f) { HoldProgress = 0f; Current.Interact(); }
            }
            else HoldProgress = Mathf.MoveTowards(HoldProgress, 0f, Time.deltaTime * 2f);
        }

        void SetCurrent(InteractPoint point)
        {
            Current = point;
            HoldProgress = 0f;
            RefreshPrompt();
            FocusChanged?.Invoke(point);
        }

        InteractPoint FindBest()
        {
            var forward = m_View != null ? Vector3.ProjectOnPlane(m_View.forward, Vector3.up).normalized : transform.forward;
            InteractPoint best = null;
            float bestScore = float.MaxValue;
            foreach (var p in InteractPoint.Active)
            {
                var to = p.FocusPoint - transform.position;
                float distance = to.magnitude;
                if (distance > p.Radius) continue;
                float angle = Vector3.Angle(forward, Vector3.ProjectOnPlane(to, Vector3.up));
                float score = distance + angle * 0.02f;      // nearer and more in front wins
                if (score < bestScore) { bestScore = score; best = p; }
            }
            return best;
        }

        void RefreshPrompt()
        {
            if (m_Prompt == null) return;
            m_Prompt.enabled = Current != null;
            m_Prompt.text = Current == null ? string.Empty : $"[{KeyLabel}] {Loc.Get(Current.PromptKey)}";
        }
    }
}
