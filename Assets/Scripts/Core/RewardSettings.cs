using UnityEngine;
namespace ScaredyCat
{
    [CreateAssetMenu(menuName = "Scaredy Cat/Reward Settings")]
    public sealed class RewardSettings : ScriptableObject
    {
        public float initialAgro = 100, knockReward = 3, breakReward = 10, ownerReward = 12, markReward = 8;
        public float knockDistance = .25f, knockAngle = 35, attributionSeconds = 6;
        public float Reward(MischiefKind kind) => kind == MischiefKind.Break ? breakReward :
            kind == MischiefKind.OwnerKnockdown ? ownerReward : kind == MischiefKind.Mark ? markReward : knockReward;
    }
}
