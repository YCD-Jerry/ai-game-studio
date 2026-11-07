using TMPro;
using UnityEngine;
namespace Game.Core
{
    [RequireComponent(typeof(TMP_Text))]
    public class LocText : MonoBehaviour
    {
        [SerializeField] string m_Key;
        public void Set(string key) { m_Key = key; Refresh(); }
        void OnEnable() { Loc.Changed += Refresh; Refresh(); }
        void OnDisable() => Loc.Changed -= Refresh;
        void Refresh() => GetComponent<TMP_Text>().text = string.IsNullOrEmpty(m_Key) ? string.Empty : Loc.Get(m_Key);
    }
}
