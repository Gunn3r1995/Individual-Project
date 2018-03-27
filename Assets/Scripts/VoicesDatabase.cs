using UnityEngine;

namespace Assets.Scripts
{
    public class VoicesDatabase : MonoBehaviour
    {
        public AudioClip[] PatrolClips;
        public AudioClip[] GuardTalkClips;
        public AudioClip[] AlertClips;
        public AudioClip[] InvestigateClips;
        public AudioClip[] InvestigateAskForHelpClips;
        public AudioClip[] InvestigateFinishClips;
        public AudioClip[] ChaseClips;
        public AudioClip[] ChaseAskForHelpClips;
        public AudioClip[] ChaseFinishClips;
        public AudioClip[] CivilianEvadeClips;
        public AudioClip[] CivilianEvadeGuardSightedClips;

        public AudioClip GetRandomPatrolClip()
        {
            if (PatrolClips == null) return null;
            if (PatrolClips.Length <= 0) return null;

            return PatrolClips[Random.Range(0, PatrolClips.Length)];
        }

        public AudioClip GetRandomGuardTalkClip()
        {
            if (GuardTalkClips == null) return null;
            if (GuardTalkClips.Length <= 0) return null;

            return GuardTalkClips[Random.Range(0, GuardTalkClips.Length)];
        }

        public AudioClip GetRandomAlertClip()
        {
            if (AlertClips == null) return null;
            if (AlertClips.Length <= 0) return null;

            return AlertClips[Random.Range(0, AlertClips.Length)];
        }

        public AudioClip GetRandomInvestigateClip()
        {
            if (InvestigateClips == null) return null;
            if (InvestigateClips.Length <= 0) return null;

            return InvestigateClips[Random.Range(0, InvestigateClips.Length)];
        }

        public AudioClip GetRandomInvestigateAskForHelpClip()
        {
            if (InvestigateAskForHelpClips == null) return null;
            if (InvestigateAskForHelpClips.Length <= 0) return null;

            return InvestigateAskForHelpClips[Random.Range(0, InvestigateAskForHelpClips.Length)];
        }

        public AudioClip GetRandomInvestigateFinishClip()
        {
            if (InvestigateFinishClips == null) return null;
            if (InvestigateFinishClips.Length <= 0) return null;

            return InvestigateFinishClips[Random.Range(0, InvestigateFinishClips.Length)];
        }

        public AudioClip GetRandomChaseClip()
        {
            if (ChaseClips == null) return null;
            if (ChaseClips.Length <= 0) return null;

            return ChaseClips[Random.Range(0, ChaseClips.Length)];
        }

        public AudioClip GetRandomChaseAskForHelpClip()
        {
            if (ChaseAskForHelpClips == null) return null;
            if (ChaseAskForHelpClips.Length <= 0) return null;

            return ChaseAskForHelpClips[Random.Range(0, ChaseAskForHelpClips.Length)];
        }

        public AudioClip GetRandomChaseFinishClip()
        {
            if (ChaseFinishClips == null) return null;
            if (ChaseFinishClips.Length <= 0) return null;

            return ChaseFinishClips[Random.Range(0, ChaseFinishClips.Length)];
        }

        public AudioClip GetRandomCivilianEvadeClip()
        {
            if (CivilianEvadeClips == null) return null;
            if (CivilianEvadeClips.Length <= 0) return null;

            return CivilianEvadeClips[Random.Range(0, CivilianEvadeClips.Length)];
        }

        public AudioClip GetRandomCivilianEvadeGuardSightedClip()
        {
            if (CivilianEvadeGuardSightedClips == null) return null;
            if (CivilianEvadeGuardSightedClips.Length <= 0) return null;

            return CivilianEvadeGuardSightedClips[Random.Range(0, CivilianEvadeGuardSightedClips.Length)];
        }
    }
}
