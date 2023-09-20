using UnityEngine;
using UnityEngine.AI;

namespace CE6127.Tanks.AI
{
    internal class ChasingState : BaseState
    {
        private TankSM m_TankSM;

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

            if (m_TankSM.Target != null)
            {
                // 1. 追踪玩家坦克
                m_TankSM.NavMeshAgent.SetDestination(m_TankSM.Target.position);

                // 2. 检查视线是否被障碍物阻挡
                RaycastHit hit;
                Vector3 directionToTarget = m_TankSM.Target.position - m_TankSM.transform.position;
                if (Physics.Raycast(m_TankSM.transform.position, directionToTarget, out hit))
                {
                    if (hit.transform == m_TankSM.Target)
                    {
                        // 3. 如果玩家坦克在一定范围内，则开火
                        if (Vector3.Distance(m_TankSM.transform.position, m_TankSM.Target.position) <= 30f) // 20是预定义的开火距离
                        {
                            m_TankSM.LaunchProjectile();
                        }
                    }
                }
            }
        }

        public override void Exit()
        {
            base.Exit();
        }
    }
}
