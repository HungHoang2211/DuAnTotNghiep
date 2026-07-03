using UnityEngine;
using UnityEngine.AI;

namespace SimpleSurvival.AI
{
    [RequireComponent(typeof(NavMeshAgent))]
    public abstract class BaseNPCController : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] protected string npcName;
        [SerializeField] protected Sprite npcPortrait;

        [Header("Interaction")]
        [SerializeField] protected float interactRange = 1.5f;

        protected NavMeshAgent _agent;

        public string NPCName => npcName;
        public Sprite Portrait => npcPortrait;
        public float InteractRange => interactRange;

        protected virtual void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
        }

        protected virtual void Start()
        {
            // NPC mặc định đứng yên
            if (_agent != null) _agent.isStopped = true;
        }

        public abstract void OnPlayerInteract(GameObject player);
    }
}