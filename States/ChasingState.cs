using UnityEngine;
using UnityEngine.AI;

namespace CE6127.Tanks.AI
{
    internal class ChasingState : BaseState
    {
        // Define class variables and constants
        private TankSM m_TankSM;
        private float detectionRadius = 40f;  // The radius within which the tank can detect other objects
        private float evadeThreshold = 20f;  // Distance threshold for evading bullets
        private float tankevadeThreshold = 2.0f;  // Distance threshold for evading other tanks
        private bool hasActed = false;  // Flag to check if the tank has taken any action
        private float inactivityTimer = 0f;  // Timer to track inactivity
        private float inactivityThreshold = 1f;  // Threshold for inactivity
        private Vector3 respawnPosition = new Vector3(0, 0, 0);  // Position to respawn the tank if inactive

        // Constructor
        public ChasingState(TankSM tankStateMachine) : base("Chasing", tankStateMachine)
        {
            m_TankSM = (TankSM)m_StateMachine;
        }

        // Called when entering this state
        public override void Enter()
        {
            base.Enter();
        }

        // Called every frame while in this state
        public override void Update()
        {
            base.Update();
            hasActed = false;

            // Check if there's a target
            if (m_TankSM.Target != null)
            {
                // Calculate distance to target
                float distanceToTarget = Vector3.Distance(m_TankSM.transform.position, m_TankSM.Target.position);
                float LaunchForce = 10f;
                RaycastHit hit;
                Vector3 directionToTarget = (m_TankSM.Target.position - m_TankSM.transform.position).normalized;
                Vector3 toAITank = m_TankSM.transform.position - m_TankSM.Target.position;
                bool isTargetVisible = Physics.Raycast(m_TankSM.transform.position, directionToTarget, out hit) && hit.transform == m_TankSM.Target;
                float angle = Vector3.Angle(m_TankSM.Target.forward, toAITank);

                // If target is visible
                if (isTargetVisible)
                {
                    hasActed = true;
                    // If close to target
                    if (distanceToTarget < 10f)
                    {    //If the enemy tank is at a short distance, make a regular attack.
                        m_TankSM.chargeTime = 0f;
                        // Rotate to face the target
                        Vector3 directionToFaceTarget = (m_TankSM.Target.position - m_TankSM.transform.position).normalized;
                        Quaternion lookRotation = Quaternion.LookRotation(directionToFaceTarget);
                        m_TankSM.transform.rotation = Quaternion.Slerp(m_TankSM.transform.rotation, lookRotation, Time.deltaTime * 5f);
                        LaunchForce = 10f;
                    }
                    // if at a medium distance, make a staged attack.
                    else if (distanceToTarget <= 50f)
                    {
                        m_TankSM.NavMeshAgent.isStopped = false;
                        m_TankSM.chargeTime += Time.deltaTime;
                        m_TankSM.chargeTime = Mathf.Min(m_TankSM.chargeTime, m_TankSM.maxChargeTime);
                        m_TankSM.NavMeshAgent.SetDestination(m_TankSM.Target.position);
                        LaunchForce = Mathf.Lerp(m_TankSM.LaunchForceMinMax.x, m_TankSM.LaunchForceMinMax.y, m_TankSM.chargeTime / m_TankSM.maxChargeTime);
                    }
                    else
                    {
                        m_TankSM.NavMeshAgent.isStopped = false;
                    }
                    m_TankSM.LaunchProjectile(LaunchForce);
                }
                else
                {
                    // If target is not visible, switch to Patrolling state
                    hasActed = true;
                    m_StateMachine.ChangeState(m_TankSM.m_States.Patrolling);
                }

                // Check for nearby bullets and other tanks
                Collider[] hitColliders = Physics.OverlapSphere(m_TankSM.transform.position, detectionRadius);
                foreach (var hitCollider in hitColliders)
                {
                    // If a bullet is detected
                    if (hitCollider.gameObject.name.Contains("Shell-VarPlayer(Clone)"))
                    {
                        float distanceToBullet = Vector3.Distance(m_TankSM.transform.position, hitCollider.transform.position);
                        if (distanceToBullet < evadeThreshold)
                        {
                            // Evade the bullet
                            EvadeBullet(hitCollider.transform.position);
                            hasActed = true;
                        }
                    }
                    // If another tank is detected
                    if (hitCollider.gameObject.name.Contains("Tank-VarSM(Clone)"))
                    {
                        float distanceToOtherTank = Vector3.Distance(m_TankSM.transform.position, hitCollider.transform.position);
                        if (distanceToOtherTank < tankevadeThreshold)
                        {
                            // Calculate a new position to avoid collision with the other tank
                            Vector3 directionToPlayer = (m_TankSM.Target.position - m_TankSM.transform.position).normalized;
                            Vector3 directionToAvoid = (m_TankSM.transform.position - hitCollider.transform.position).normalized;
                            Vector3 blendedDirection = (directionToPlayer + directionToAvoid).normalized;

                            float OffsetDistance = 5.0f;
                            Vector3 newPosition = m_TankSM.transform.position + blendedDirection * OffsetDistance;

                            m_TankSM.NavMeshAgent.SetDestination(newPosition);
                        }
                    }
                }

                // Check for inactivity
                if (hasActed)
                {
                    inactivityTimer = 0f;
                }
                else
                {
                    inactivityTimer += Time.deltaTime;
                }
                if (inactivityTimer >= inactivityThreshold)
                {
                    // Respawn the tank if inactive for too long
                    m_TankSM.transform.position = respawnPosition;
                    inactivityTimer = 0f;
                    Debug.Log("Tank respawned due to inactivity.");
                }

            }
            else
            {
                // If no target is found, switch to Patrolling state
                Debug.Log("Target is null, switching to Patrolling state");
                m_StateMachine.ChangeState(m_TankSM.m_States.Patrolling);
            }
        }

        // Method to evade bullets
        void EvadeBullet(Vector3 bulletPosition)
        {
            Vector3 directionToBullet = bulletPosition - m_TankSM.transform.position;
            Vector3 evadeDirection = Vector3.Cross(directionToBullet, Vector3.up).normalized;

            m_TankSM.NavMeshAgent.velocity = evadeDirection * m_TankSM.NavMeshAgent.speed;

            // Rotate to face the player tank after evading
            Vector3 directionToFaceTarget = (m_TankSM.Target.position - m_TankSM.transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(directionToFaceTarget);
            m_TankSM.transform.rotation = lookRotation;
        }

        // Called when exiting this state
        public override void Exit()
        {
            base.Exit();
        }
    }
}
