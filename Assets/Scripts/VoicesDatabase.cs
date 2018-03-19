using UnityEngine;

namespace Assets.Scripts
{
    public class VoicesDatabase : MonoBehaviour
    {
        public AudioClip[] PatrolClips;
        public AudioClip[] GuardTalkClips;
        public AudioClip[] AlertClips;
        public AudioClip[] InvestigateClips;
        public AudioClip[] ChaseClips;

        public AudioClip GetRandomPatrolClips()
        {
            if (PatrolClips == null) return null;
            if (PatrolClips.Length <= 0) return null;

            return PatrolClips[Random.Range(0, PatrolClips.Length)];
        }

        public AudioClip GetRandomGuardTalkClips()
        {
            if (GuardTalkClips == null) return null;
            if (GuardTalkClips.Length <= 0) return null;

            return GuardTalkClips[Random.Range(0, GuardTalkClips.Length)];
        }

        public AudioClip GetRandomAlertClips()
        {
            if (AlertClips == null) return null;
            if (AlertClips.Length <= 0) return null;

            return AlertClips[Random.Range(0, AlertClips.Length)];
        }

        public AudioClip GetRandomInvestigateClips()
        {
            if (InvestigateClips == null) return null;
            if (InvestigateClips.Length <= 0) return null;

            return InvestigateClips[Random.Range(0, InvestigateClips.Length)];
        }

        public AudioClip GetRandomChaseClips()
        {
            if (ChaseClips == null) return null;
            if (ChaseClips.Length <= 0) return null;

            return ChaseClips[Random.Range(0, ChaseClips.Length)];
        }
    }
}
