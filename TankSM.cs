using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using System.Collections;

// Use Random and Debug from UnityEngine namespace
using Random = UnityEngine.Random;
using Debug = UnityEngine.Debug;

namespace CE6127.Tanks.AI
{
    /// <summary>
    /// Class <c>TankSM</c> represents the state machine for the tank.
    /// </summary>
    internal class TankSM : StateMachine
    {
        // Flags to determine if the tank has fired or if the round was playing
        private bool m_Fired = false;
        private bool m_WasRoundPlaying = false;

        // Cooldown time between shots and charge time for the shot
        public float CooldownTime = 0.35f;
        public float chargeTime = 0f;
        public float maxChargeTime = 0.5f;

        // Struct to hold the different states the tank can be in
        protected internal struct States
        {
            public IdleState Idle;
            public PatrollingState Patrolling;
            public ChasingState Chasing;

            internal States(TankSM sm)
            {
                Idle = new IdleState(sm);
                Patrolling = new PatrollingState(sm);
                Chasing = new ChasingState(sm);
            }
        }

        // Instance of the states struct
        public States m_States;

        // Various properties related to the tank's behavior and its references
        [HideInInspector] public GameManager GameManager;
        [HideInInspector] public NavMeshAgent NavMeshAgent;
        [Header("Patrolling")]
        [Tooltip("Minimum and maximum time delay for patrolling wait.")]
        public Vector2 PatrolWaitTime = new(1.5f, 3.5f);
        [Tooltip("Minimum and maximum circumradius of the area to patrol at a given update time.")]
        public Vector2 PatrolMaxDist = new(15f, 30f);
        [Range(0f, 2f)] public float PatrolNavMeshUpdate = 0.1f;
        [Header("Targeting")]
        [Tooltip("Minimum and maximum range for the targeting range.")]
        public Vector2 StartToTargetDist = new(28f, 35f);
        [HideInInspector] public float TargetDistance;
        [Tooltip("Minimum and maximum range for the stopping range.")]
        public Vector2 StopAtTargetDist = new(18f, 22f);
        [HideInInspector] public float StopDistance;
        [Range(0f, 2f)] public float TargetNavMeshUpdate = 0.1f;
        [Header("Blending")]
        [Range(0f, 1f)] public float OrientSlerpScalar = 0.2f;
        [HideInInspector] public Transform Target;
        [HideInInspector] public float NavMeshUpdateDeadline;
        [Header("Firing")]
        [Tooltip("Minimum and maximum cooldown time delay between each firing in seconds.")]
        public Vector2 FireInterval = new(0.7f, 2.5f);
        [Tooltip("Force given to the shell if the fire button is not held, and the force given to the shell if the fire button is held for the max charge time in seconds.")]
        public Vector2 LaunchForceMinMax = new(7.5f, 30f);
        [Header("References")]
        [Tooltip("Prefab")] public Rigidbody Shell;
        [Tooltip("Transform")] public Transform FireTransform;
        [Header("Firing Audio")]
        public AudioSource SFXAudioSource;
        public AudioClip ShotFiringAudioClip;

        // Flags to determine if the tank has started and its references
        private bool m_Started = false;
        private Rigidbody m_Rigidbody;
        private TankSound m_TankSound;

        /// <summary>
        /// Returns the current tank's velocity.
        /// </summary>
        private Vector2 MoveTurnSound() => new Vector2(Mathf.Abs(NavMeshAgent.velocity.x), Mathf.Abs(NavMeshAgent.velocity.z));

        /// <summary>
        /// Returns the initial state of the state machine.
        /// </summary>
        protected override BaseState GetInitialState() => m_States.Idle;

        /// <summary>
        /// Sets the NavMeshAgent's speed and angular speed.
        /// </summary>
        private void SetNavMeshAgent()
        {
            NavMeshAgent.speed = GameManager.Speed;
            NavMeshAgent.angularSpeed = GameManager.AngularSpeed;
        }

        /// <summary>
        /// Sets the NavMeshAgent's stopping distance to zero.
        /// </summary>
        public void SetStopDistanceToZero() => NavMeshAgent.stoppingDistance = 0f;

        /// <summary>
        /// Sets the NavMeshAgent's stopping distance to the target's distance.
        /// </summary>
        public void SetStopDistanceToTarget() => NavMeshAgent.stoppingDistance = StopDistance;

        /// <summary>
        /// Called when the script instance is being loaded.
        /// </summary>
        private void Awake()
        {
            m_States = new States(this);
            GameManager = GameManager.Instance;
            m_Rigidbody = GetComponent<Rigidbody>();
            NavMeshAgent = GetComponent<NavMeshAgent>();
            m_TankSound = GetComponent<TankSound>();
            SetNavMeshAgent();
            TargetDistance = Random.Range(StartToTargetDist.x, StartToTargetDist.y);
            StopDistance = Random.Range(StopAtTargetDist.x, StopAtTargetDist.y);
            SetStopDistanceToTarget();
            var tankManagers = GameManager.PlayerPlatoon.Tanks.Take(1);
            if (tankManagers.Count() != 0)
                Target = tankManagers.First().Instance.transform;
            else
                Debug.LogError("'Player Platoon' is empty!");
        }

        /// <summary>
        /// Called when the object becomes enabled and active.
        /// </summary>
        private void OnEnable()
        {
            m_Rigidbody.isKinematic = false;
        }

        /// <summary>
        /// Called on the frame when a script is enabled just before any of the Update methods are called the first time.
        /// </summary>
        private new void Start()
        {
            m_TankSound.MoveTurnInputCalc += MoveTurnSound;
        }

        /// <summary>
        /// Called when the behaviour becomes disabled or inactive.
        /// </summary>
        private void OnDisable()
        {
            m_Rigidbody.isKinematic = true;
            m_TankSound.MoveTurnInputCalc -= MoveTurnSound;
        }

        /// <summary>
        /// Called every frame, if the MonoBehaviour is enabled.
        /// </summary>
        private new void Update()
        {
            if (!m_Started && GameManager.IsRoundPlaying)
            {
                m_Started = true;
                base.Start();
            }
            else if (GameManager.IsRoundPlaying)
            {
                base.Update();
            }
            else
            {
                m_Started = false;
                StopAllCoroutines();
            }
            if (!m_WasRoundPlaying && GameManager.IsRoundPlaying)
            {
                m_Fired = false;
            }
            m_WasRoundPlaying = GameManager.IsRoundPlaying;
        }

        /// <summary>
        /// Instantiate and launch the shell.
        /// </summary>
        public void LaunchProjectile(float LaunchForce)
        {
            if (m_Fired)
            {
                return;
            }
            m_Fired = true;   //Force tanks into attack cooldowns to avoid unfair gameplay
            Rigidbody shellInstance = Instantiate(Shell, FireTransform.position, FireTransform.rotation) as Rigidbody;
            shellInstance.velocity = LaunchForce * FireTransform.forward;
            SFXAudioSource.clip = ShotFiringAudioClip;
            SFXAudioSource.Play();
            StartCoroutine(ResetFiredFlag());
        }

        /// <summary>
        /// Reset the fired flag after a cooldown.
        /// </summary>
        private IEnumerator ResetFiredFlag()
        {
            yield return new WaitForSeconds(CooldownTime = 1f);
            m_Fired = false;
        }
    }
}
