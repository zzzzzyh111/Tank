using UnityEngine;

// Use Debug from UnityEngine namespace
using Debug = UnityEngine.Debug;

namespace CE6127.Tanks.AI
{
    /// <summary>
    /// Class <c>IdleState</c> represents the state of the tank when it is idle.
    /// </summary>
    internal class IdleState : BaseState
    {
        // Reference to the tank's state machine
        private TankSM m_TankSM;

        /// <summary>
        /// Constructor for the IdleState class.
        /// </summary>
        /// <param name="tankStateMachine">The state machine associated with the tank.</param>
        public IdleState(TankSM tankStateMachine) : base("Idle", tankStateMachine) => m_TankSM = (TankSM)m_StateMachine;

        /// <summary>
        /// Called when the tank enters the idle state.
        /// </summary>
        public override void Enter() => base.Enter();

        /// <summary>
        /// Called every frame to update the tank's behavior in the idle state.
        /// </summary>
        public override void Update()
        {
            base.Update();

            // Check if the tank has a target
            if (m_TankSM.Target != null)
            {
                // Calculate the distance to the target
                var dist = Vector3.Distance(m_TankSM.transform.position, m_TankSM.Target.position);

                // If the target is beyond a certain distance, switch to patrolling state
                if (dist > m_TankSM.TargetDistance)
                    m_StateMachine.ChangeState(m_TankSM.m_States.Patrolling);

                // If the target is within a certain range, switch to chasing state
                if (dist <= 70f)
                    m_StateMachine.ChangeState(m_TankSM.m_States.Chasing);

                // Additional behaviors can be added here
                // ... Just for demonstration purposes; more to be implemented.
            }

            // Orient the tank to face its target
            var lookPos = m_TankSM.Target.position - m_TankSM.transform.position;
            lookPos.y = 0f;
            var rot = Quaternion.LookRotation(lookPos);
            m_TankSM.transform.rotation = Quaternion.Slerp(m_TankSM.transform.rotation, rot, m_TankSM.OrientSlerpScalar);
        }
    }
}
