using UnityEngine;
using UnityEngine.AI;

namespace CE6127.Tanks.AI
{
    internal class ChasingState : BaseState
    {
        private TankSM m_TankSM;
        private float detectionRadius = 40f;  
        private float evadeThreshold = 20f;  
        private float tankevadeThreshold = 2.0f;
        private bool hasActed = false;  
        private float inactivityTimer = 0f;  
        private float inactivityThreshold = 1f;  
        private Vector3 respawnPosition = new Vector3(0, 0, 0);  

        public ChasingState(TankSM tankStateMachine) : base("Chasing", tankStateMachine)
        {
            m_TankSM = (TankSM)m_StateMachine;
        }

        public override void Enter()
        {
            base.Enter();
        }

        public override void Update()
        {
            base.Update();
            hasActed = false;

            if (m_TankSM.Target != null)
            {
                float distanceToTarget = Vector3.Distance(m_TankSM.transform.position, m_TankSM.Target.position);
                float LaunchForce = 10f;
                RaycastHit hit;
                Vector3 directionToTarget = (m_TankSM.Target.position - m_TankSM.transform.position).normalized;
                Vector3 toAITank = m_TankSM.transform.position - m_TankSM.Target.position;
                bool isTargetVisible = Physics.Raycast(m_TankSM.transform.position, directionToTarget, out hit) && hit.transform == m_TankSM.Target;
                float angle = Vector3.Angle(m_TankSM.Target.forward, toAITank);
               
                if (isTargetVisible)
                {
                    hasActed = true;
                    if (distanceToTarget < 10f) 
                    {
                    
                        // m_TankSM.NavMeshAgent.isStopped = true;
                        m_TankSM.chargeTime = 0f;
                        Vector3 directionToFaceTarget = (m_TankSM.Target.position - m_TankSM.transform.position).normalized;
                        Quaternion lookRotation = Quaternion.LookRotation(directionToFaceTarget);
                        m_TankSM.transform.rotation = Quaternion.Slerp(m_TankSM.transform.rotation, lookRotation, Time.deltaTime * 5f); 
                        LaunchForce = 10f;
                    }
                
                
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
                    // Track Player Tank
                    hasActed = true;
                    m_StateMachine.ChangeState(m_TankSM.m_States.Patrolling);
                    // Debug.Log("Patrolling!");


                }
                // if (angle < 15.0f)
                //     {
                        
                //         Vector3 evadeDirection = Vector3.Cross(m_TankSM.Target.forward, Vector3.up);
                //         m_TankSM.NavMeshAgent.SetDestination(m_TankSM.transform.position + evadeDirection);
                //     }

                 // Evade Bullets
                Collider[] hitColliders = Physics.OverlapSphere(m_TankSM.transform.position, detectionRadius);
                foreach (var hitCollider in hitColliders)
                {
                    // Debug.Log("Collider name: " + hitCollider.gameObject.name);
                    if (hitCollider.gameObject.name.Contains("Shell-VarPlayer(Clone)"))
                    {
                        // Calculate distance to bullets
                        float distanceToBullet = Vector3.Distance(m_TankSM.transform.position, hitCollider.transform.position);
                        // Debug.Log("Bullet Detected!");
                        // Evade bullets if distance is smaller than the threshold
                        if (distanceToBullet < evadeThreshold)
                        {
                            EvadeBullet(hitCollider.transform.position);
                            hasActed = true;
                        // Debug.Log("Evasion Executed!");
                        }
                    }
                    if (hitCollider.gameObject.name.Contains("Tank-VarSM(Clone)"))
                    {
                        // Debug.Log("Tank Detected!");
                        float distanceToOtherTank = Vector3.Distance(m_TankSM.transform.position, hitCollider.transform.position);
                        if (distanceToOtherTank < tankevadeThreshold)
                        {
                            Vector3 directionToPlayer = (m_TankSM.Target.position - m_TankSM.transform.position).normalized;
                            Vector3 directionToAvoid = (m_TankSM.transform.position - hitCollider.transform.position).normalized;
                            Vector3 blendedDirection = (directionToPlayer + directionToAvoid).normalized;

                            // Calculate the new position based on the blended direction
                            float OffsetDistance = 5.0f;  // You can adjust this value
                            Vector3 newPosition = m_TankSM.transform.position + blendedDirection * OffsetDistance;

                            // Set the new destination
                            m_TankSM.NavMeshAgent.SetDestination(newPosition);
                            // Debug.Log("Offset Executed!");

                        }
                    }
                }
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
                    // Respan the tank
                    m_TankSM.transform.position = respawnPosition;
                    inactivityTimer = 0f;
                    Debug.Log("Tank respawned due to inactivity.");
                }

            }
            else
            {
                Debug.Log("Target is null, switching to Patrolling state");
                m_StateMachine.ChangeState(m_TankSM.m_States.Patrolling);
            }
        }

        void EvadeBullet(Vector3 bulletPosition)
        {
            // Evade the bullet
            Vector3 directionToBullet = bulletPosition - m_TankSM.transform.position;
            Vector3 evadeDirection = Vector3.Cross(directionToBullet, Vector3.up).normalized;

            // Use velocity for faster movement
            m_TankSM.NavMeshAgent.velocity = evadeDirection * m_TankSM.NavMeshAgent.speed;

            // Turn back to face the player tank immediately after evading
            Vector3 directionToFaceTarget = (m_TankSM.Target.position - m_TankSM.transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(directionToFaceTarget);
            m_TankSM.transform.rotation = lookRotation;
        }


        public override void Exit()
        {
            base.Exit();
        }
    }
}
