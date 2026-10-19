using UnityEngine;

namespace StarShift.Encounters
{
    [CreateAssetMenu(menuName = "StarShift/Encounters/Formation Plan")]
    public sealed class FormationPlan : ScriptableObject
    {
        public Vector2[] Slots = System.Array.Empty<Vector2>();

        public bool IsValid
        {
            get
            {
                if (Slots == null || Slots.Length == 0) return false;
                for (int i = 0; i < Slots.Length; i++)
                {
                    if (!ConfigValue.Finite(Slots[i])) return false;
                    for (int j = i + 1; j < Slots.Length; j++)
                        if ((Slots[i] - Slots[j]).sqrMagnitude < .000001f) return false;
                }
                return true;
            }
        }
    }
}
