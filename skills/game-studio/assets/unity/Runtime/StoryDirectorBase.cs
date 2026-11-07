using System;
using System.Collections;
using UnityEngine;
using Game.Core;
using Game.Gameplay;

namespace Game.Narrative
{
    public interface IPlayerMotor { bool InputEnabled { get; set; } }
    public interface ICameraLook { bool LookEnabled { get; set; } }
    public interface ISubtitleView
    {
        /// <summary>Shows one line; waits for Advance when waitForKey, else for readSeconds. Must return early when skipped.</summary>
        IEnumerator ShowLine(string speaker, string text, bool waitForKey, float readSeconds);
        void Hide();
        void ShowHint(string text);
    }

    /// <summary>Base for every chapter's director: the story is one coroutine in the subclass's Start().</summary>
    public abstract class StoryDirectorBase : MonoBehaviour
    {
        [SerializeField] MonoBehaviour m_PlayerMotor;   // implements IPlayerMotor
        [SerializeField] MonoBehaviour m_CameraLook;    // implements ICameraLook
        [SerializeField] MonoBehaviour m_Subtitles;     // implements ISubtitleView
        [SerializeField] protected Transform m_Player;
        [SerializeField] protected PlayerInteractor m_Interactor;
        [SerializeField] float m_HintDelay = 60f;
        [SerializeField] float m_TurnSeconds = 0.4f;

        protected IPlayerMotor Player { get; private set; }
        protected ICameraLook Look { get; private set; }
        protected ISubtitleView View { get; private set; }

        public readonly struct Line
        {
            public readonly string SpeakerKey, TextKey;   // speaker key "spk_<role>", or null for narration
            public Line(string speakerKey, string textKey) { SpeakerKey = speakerKey; TextKey = textKey; }
        }
        protected static Line L(string speakerKey, string textKey) => new(speakerKey, textKey);

        protected virtual void Awake()
        {
            Player = m_PlayerMotor as IPlayerMotor ?? throw new MissingReferenceException($"{name}: m_PlayerMotor must implement IPlayerMotor");
            Look = m_CameraLook as ICameraLook ?? throw new MissingReferenceException($"{name}: m_CameraLook must implement ICameraLook");
            View = m_Subtitles as ISubtitleView ?? throw new MissingReferenceException($"{name}: m_Subtitles must implement ISubtitleView");
        }

        /// <summary>Holds the player still (or frees them). The camera stays the player's either way: never take the view away.</summary>
        protected void SetPlayerControl(bool enabled)
        {
            Player.InputEnabled = enabled;
            if (m_Interactor != null) m_Interactor.enabled = enabled;
            Look.LookEnabled = true;
        }

        protected bool PlayerCanAct => Player.InputEnabled && Time.timeScale > 0f;

        /// <summary>People talking to each other: they face each other; each line waits for Advance.</summary>
        protected IEnumerator Conversation(params Line[] lines) => Spoken(lines, true, true);
        /// <summary>Thoughts, voices, narration: nobody turns; each line waits for Advance.</summary>
        protected IEnumerator Monologue(params Line[] lines) => Spoken(lines, false, true);
        /// <summary>Small talk while the player walks: always auto-advances (nothing tells them to press a key).</summary>
        protected IEnumerator Musing(params Line[] lines) => Spoken(lines, true, false);

        IEnumerator Spoken(Line[] lines, bool staged, bool waitForKey)
        {
            Transform previous = null;
            try
            {
                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i];
                    string text = Loc.Get(line.TextKey);
                    var body = BodyOf(line.SpeakerKey);
                    if (staged && body != null)
                    {
                        var listener = previous != null && previous != body ? previous : NextBody(lines, i, body) ?? (body != m_Player ? m_Player : null);
                        if (listener != null)
                        {
                            StartCoroutine(Turn(body, listener.position));
                            StartCoroutine(Turn(listener, body.position));
                        }
                    }
                    yield return View.ShowLine(line.SpeakerKey != null ? Loc.Get(line.SpeakerKey) : null, text, waitForKey, ReadSeconds(text));
                    if (body != null) previous = body;
                }
            }
            finally { View.Hide(); }
        }

        Transform NextBody(Line[] lines, int from, Transform not)
        {
            for (int j = from + 1; j < lines.Length; j++)
                if (BodyOf(lines[j].SpeakerKey) is { } b && b != not) return b;
            return null;
        }

        Transform BodyOf(string speakerKey)
        {
            int at = speakerKey?.IndexOf("spk_", StringComparison.Ordinal) ?? -1;
            return at < 0 ? null : TalkBodyOf(speakerKey.Substring(at + 4));
        }

        /// <summary>The body behind "spk_&lt;role&gt;". Each chapter adds its cast; null = a voice (use Monologue).</summary>
        protected virtual Transform TalkBodyOf(string role) => role == "player" ? m_Player : null;

        /// <summary>Body turn toward a point. The character the player is walking keeps its heading (head-only turns belong to an IK/look-at component).</summary>
        IEnumerator Turn(Transform who, Vector3 point)
        {
            if (who == m_Player && Player.InputEnabled) yield break;
            var flat = Vector3.ProjectOnPlane(point - who.position, Vector3.up);
            if (flat.sqrMagnitude < 0.01f) yield break;
            var from = who.rotation;
            var to = Quaternion.LookRotation(flat);
            for (float t = 0f; t < m_TurnSeconds; t += Time.deltaTime)
            {
                who.rotation = Quaternion.Slerp(from, to, Mathf.SmoothStep(0f, 1f, t / m_TurnSeconds));
                yield return null;
            }
            who.rotation = to;
        }

        /// <summary>Reading time: about 6 CJK characters or 20 Latin characters a second, clamped.</summary>
        public static float ReadSeconds(string text)
        {
            if (string.IsNullOrEmpty(text)) return 3f;
            int wide = 0;
            foreach (char c in text) if (c >= 0x2E80) wide++;
            return Mathf.Clamp(1.2f + wide * 0.16f + (text.Length - wide) * 0.05f, 2.2f, 9f);
        }

        /// <summary>Waits for the interact key on target; says the hint once if the player wanders for too long.</summary>
        protected IEnumerator WaitForInteract(InteractPoint target, string hintKey = null)
        {
            bool done = false;
            void OnUsed(InteractPoint _) => done = true;
            target.enabled = true;
            target.Interacted += OnUsed;
            try
            {
                for (float waited = 0f; !done; waited += Time.deltaTime)
                {
                    if (hintKey != null && waited > m_HintDelay) { View.ShowHint(Loc.Get(hintKey)); hintKey = null; }
                    yield return null;
                }
            }
            finally { target.Interacted -= OnUsed; target.enabled = false; }
        }
    }
}
