using System.Collections;
using UnityEngine;
using Game.Gameplay;
using Game.Narrative;

// Example chapter director: copy, rename, and add your cast and beats.
public class Level01Director : StoryDirectorBase
{
    [SerializeField] InteractPoint m_Door;
    [SerializeField] Transform m_Guide;
    protected override Transform TalkBodyOf(string role) => role switch { "guide" => m_Guide, _ => base.TalkBodyOf(role) };
    IEnumerator Start()
    {
        SetPlayerControl(false);
        yield return Conversation(L("l1_spk_guide", "l1_guide_001"), L("spk_player", "l1_player_001"));
        SetPlayerControl(true);
        yield return WaitForInteract(m_Door, "l1_hint_door");
        yield return Musing(L("l1_spk_guide", "l1_guide_002"));
    }
}
