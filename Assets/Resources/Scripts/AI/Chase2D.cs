#if GRAPH_DESIGNER
using Opsive.BehaviorDesigner.Runtime.Tasks;
using Opsive.BehaviorDesigner.Runtime.Tasks.Actions;
using Opsive.GraphDesigner.Runtime.Variables;
using UnityEngine;

[Opsive.Shared.Utility.Category("2D Enemy")]
[Opsive.Shared.Utility.Description("Đuổi theo Target theo trục X, quay mặt về phía Target. Trả về Success khi đã đến gần hơn Stop Distance.")]
public class Chase2D : Action
{
    [Tooltip("Đối tượng cần đuổi (thường là Player).")]
    [SerializeField] protected SharedVariable<GameObject> m_Target;
    [Tooltip("Tốc độ đuổi.")]
    [SerializeField] protected SharedVariable<float> m_Speed = 3f;
    [Tooltip("Dừng lại khi khoảng cách ngang tới Target nhỏ hơn giá trị này (nên gán cùng biến với tầm đánh).")]
    [SerializeField] protected SharedVariable<float> m_StopDistance = 1f;

    private Rigidbody2D m_Rigidbody;

    public override void OnAwake()
    {
        m_Rigidbody = gameObject.GetComponent<Rigidbody2D>();
    }

    public override TaskStatus OnUpdate()
    {
        var target = m_Target.Value;
        if (target == null)
            return TaskStatus.Failure;

        float dx = target.transform.position.x - transform.position.x;
        Facing2D.Set(transform, dx);
        if (Mathf.Abs(dx) < m_StopDistance.Value)
        {
            Facing2D.MoveX(transform, m_Rigidbody, 0f, 0f);
            return TaskStatus.Success;
        }

        Facing2D.MoveX(transform, m_Rigidbody, Mathf.Sign(dx), m_Speed.Value);
        return TaskStatus.Running;
    }

    public override void OnEnd()
    {
        Facing2D.MoveX(transform, m_Rigidbody, 0f, 0f);
    }
}
#endif
