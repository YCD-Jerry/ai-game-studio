using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Gameplay
{
    /// <summary>Something usable with the interact key. Availability is just `enabled`, so directors toggle it as the story moves.</summary>
    public class InteractPoint : MonoBehaviour
    {
        static readonly List<InteractPoint> s_Active = new();
        public static IReadOnlyList<InteractPoint> Active => s_Active;

        [SerializeField] string m_PromptKey = "ui_use";
        [SerializeField] bool m_Hold;
        [SerializeField] float m_HoldSeconds = 1.2f;
        [SerializeField] float m_Radius = 2f;
        [SerializeField] Transform m_Focus;
        [SerializeField] UnityEvent m_OnInteract = new();

        public event Action<InteractPoint> Interacted;
        public string PromptKey => m_PromptKey;
        public bool Hold => m_Hold;
        public float HoldSeconds => m_HoldSeconds;
        public float Radius => m_Radius;
        public Vector3 FocusPoint => m_Focus != null ? m_Focus.position : transform.position;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => s_Active.Clear();

        void OnEnable() => s_Active.Add(this);
        void OnDisable() => s_Active.Remove(this);

        public void Interact()
        {
            Interacted?.Invoke(this);
            m_OnInteract.Invoke();
        }
    }
}
