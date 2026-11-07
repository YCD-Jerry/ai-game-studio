using Unity.AI.Navigation;
using UnityEngine;

namespace Game.World
{
    /// <summary>Generated scenes are rebuilt often: bake on load instead of storing NavMesh data that goes stale.</summary>
    [DefaultExecutionOrder(-500)]
    [RequireComponent(typeof(NavMeshSurface))]
    public class RuntimeNavMeshBaker : MonoBehaviour
    {
        void Awake() => GetComponent<NavMeshSurface>().BuildNavMesh();
    }
}
