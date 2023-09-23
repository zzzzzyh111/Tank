using System.Collections;
using UnityEngine;

// Use Random and Debug from UnityEngine namespace
using Random = UnityEngine.Random;
using Debug = UnityEngine.Debug;

namespace CE6127.Tanks.AI
{
    /// <summary>
    /// Class <c>PatrollingState</c> represents the state of the tank when it is patrolling.
    /// </summary>
    internal class PatrollingState : BaseState
    {
        // Reference to the tank's state machine
        private TankSM m_TankSM;

        // Destination point for the tank during patrolling
        private Vector3 m_Destination;

        /// <summary>
        /// Constructor for the PatrollingState class.
        /// </summary>
        public PatrollingState(TankSM tankStateMachine) : base("Patrolling", tankStateMachine) => m_TankSM = (TankSM)m_StateMachine;

        /// <summary>
        /// Called when the tank enters the patrolling state.
        /// </summary>
        public override void Enter()
        {
            base.Enter();

            // Set the stopping distance to zero
            m_TankSM.SetStopDistanceToZero();

            // Start the patrolling coroutine
            m_TankSM.StartCoroutine(Patrolling());
        }

        /// <summary>
        /// Called every frame to update the tank's behavior in the patrolling state.
        /// </summary>
        public override void Update()
        {
            base.Update();

            // Check if the tank has a target
            if (m_TankSM.Target != null)
            {
                // Calculate the distance to the target
                var dist = Vector3.Distance(m_TankSM.transform.position, m_TankSM.Target.position);

                // If the target is within a certain range, switch to chasing state
                if (dist <= 70f)
                    m_StateMachine.ChangeState(m_TankSM.m_States.Chasing);
            }

            // Update the destination for the NavMeshAgent at regular intervals
            if (Time.time >= m_TankSM.NavMeshUpdateDeadline)
            {
                m_TankSM.NavMeshUpdateDeadline = Time.time + m_TankSM.PatrolNavMeshUpdate;
                m_TankSM.NavMeshAgent.SetDestination(m_Destination);
            }
        }

        /// <summary>
        /// Called when the tank exits the patrolling state.
        /// </summary>
        public override void Exit()
        {
            base.Exit();

            // Stop the patrolling coroutine
            m_TankSM.StopCoroutine(Patrolling());
        }

        /// <summary>
        /// Coroutine to handle the patrolling behavior.
        /// </summary>
        IEnumerator Patrolling()
        {
            while (true)
            {
                // Randomly determine the next destination within a certain range
                var destination = Random.insideUnitCircle * Random.Range(m_TankSM.PatrolMaxDist.x, m_TankSM.PatrolMaxDist.y);
                m_Destination = m_TankSM.transform.position + new Vector3(destination.x, 0f, destination.y);

                // Wait for a random amount of time before moving to the next destination
                float waitInSec = Random.Range(m_TankSM.PatrolWaitTime.x, m_TankSM.PatrolWaitTime.y);
                yield return new WaitForSeconds(waitInSec);
            }
        }
    }
}
