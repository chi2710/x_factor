#if GRAPH_DESIGNER
using Opsive.BehaviorDesigner.Runtime.Tasks;
using Opsive.BehaviorDesigner.Runtime.Tasks.Actions;
using Opsive.GraphDesigner.Runtime.Variables;
using UnityEngine;

[Opsive.Shared.Utility.Category("2D Enemy")]
[Opsive.Shared.Utility.Description("Đi tuần ngang quanh vị trí xuất phát, trong khoảng ±Distance. Mỗi lần chạy đi một chặng tới một đầu rồi trả về Success; lần sau đi về đầu kia.")]
public class Patrol2D : Action
{
    [Tooltip("Khoảng cách tối đa tính từ vị trí xuất phát.")]
    [SerializeField] protected SharedVariable<float> m_Distance = 4f;
    [Tooltip("Tốc độ đi tuần.")]
    [SerializeField] protected SharedVariable<float> m_Speed = 1.5f;
    [Tooltip("Cách điểm đến bao nhiêu thì coi là đã tới.")]
    [SerializeField] protected SharedVariable<float> m_ArriveDistance = 0.05f;

    private Rigidbody2D m_Rigidbody;
    private float m_OriginX;
    private float m_Dir;
    private float m_TargetX;

    public override void OnAwake()
    {
        m_Rigidbody = gameObject.GetComponent<Rigidbody2D>();
        m_OriginX = transform.position.x;
        m_Dir = Facing2D.Get(transform);
    }

    public override void OnStart()
    {
        m_TargetX = m_OriginX + m_Dir * m_Distance.Value;
    }

    public override TaskStatus OnUpdate()
    {
        float dx = m_TargetX - transform.position.x;
        if (Mathf.Abs(dx) <= m_ArriveDistance.Value)
        {
            Facing2D.MoveX(transform, m_Rigidbody, 0f, 0f);
            m_Dir = -m_Dir;
            return TaskStatus.Success;
        }

        float dir = Mathf.Sign(dx);
        Facing2D.Set(transform, dir);
        Facing2D.MoveX(transform, m_Rigidbody, dir, m_Speed.Value);
        return TaskStatus.Running;
    }

    public override void OnEnd()
    {
        Facing2D.MoveX(transform, m_Rigidbody, 0f, 0f);
    }
}
#endif
