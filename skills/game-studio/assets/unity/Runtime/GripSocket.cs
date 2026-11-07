using UnityEngine;

namespace Game.Gameplay
{
    /// <summary>
    /// A hand socket that cancels the bone's scale, so props parented to it keep their real size.
    /// Rigs from AI-3D tools with Mixamo skeletons often carry a centimetre armature scale: the hand bone's
    /// lossyScale was about 0.0145, so a prop parented straight to the hand shrank to 1.5% and vanished,
    /// and local offsets were in centimetres. Put this on an empty child of the hand bone and parent props to it.
    /// </summary>
    [ExecuteAlways]
    public class GripSocket : MonoBehaviour
    {
        [Tooltip("Optional: the index finger's first bone. The palm sits about 60% of the way from wrist to it.")]
        [SerializeField] Transform m_IndexProximal;
        [Range(0f, 1f)] [SerializeField] float m_PalmFraction = 0.6f;

        /// <summary>Creates a socket under the hand bone, scaled to cancel the bone's scale and placed on the palm.</summary>
        public static GripSocket Create(Transform hand, Transform indexProximal = null, string name = "Grip")
        {
            var go = new GameObject(name);
            go.transform.SetParent(hand, false);
            var socket = go.AddComponent<GripSocket>();
            socket.m_IndexProximal = indexProximal;
            socket.Fit();
            return socket;
        }

        /// <summary>Recomputes scale and palm position. Call after the rig's scale changes.</summary>
        public void Fit()
        {
            var hand = transform.parent;
            if (hand == null) return;
            var s = hand.lossyScale;
            transform.localScale = new Vector3(1f / NonZero(s.x), 1f / NonZero(s.y), 1f / NonZero(s.z));
            transform.position = m_IndexProximal != null
                ? Vector3.Lerp(hand.position, m_IndexProximal.position, m_PalmFraction)
                : hand.position;
        }

        /// <summary>Parents a prop to the socket at a world-space offset and rotation, keeping its real size.</summary>
        public void Hold(Transform prop, Vector3 localOffset, Quaternion localRotation)
        {
            prop.SetParent(transform, false);
            prop.localPosition = localOffset;
            prop.localRotation = localRotation;
        }

        /// <summary>True when the socket's world scale is 1 within tolerance (a data check for audits).</summary>
        public bool IsUnitScale(float tolerance = 0.01f)
        {
            var s = transform.lossyScale;
            return Mathf.Abs(s.x - 1f) < tolerance && Mathf.Abs(s.y - 1f) < tolerance && Mathf.Abs(s.z - 1f) < tolerance;
        }

        static float NonZero(float v) => Mathf.Abs(v) < 1e-6f ? 1f : v;

        void OnValidate() => Fit();
    }
}
