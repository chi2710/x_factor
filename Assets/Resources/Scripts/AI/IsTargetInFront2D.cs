#if GRAPH_DESIGNER
using Opsive.BehaviorDesigner.Runtime.Tasks;
using Opsive.BehaviorDesigner.Runtime.Tasks.Conditionals;
using Opsive.GraphDesigner.Runtime.Variables;
using UnityEngine;

[Opsive.Shared.Utility.Category("2D Enemy")]
[Opsive.Shared.Utility.Description("Success nếu Target nằm phía trước mặt (theo trục X) của nhân vật này.")]
public class IsTargetInFront2D : Conditional
{
    [Tooltip("Đối tượng cần kiểm tra (thường là Player).")]
    [SerializeField] protected SharedVariable<GameObject> m_Target;

    public override TaskStatus OnUpdate()
    {
        var target = m_Target.Value;
        if (target == null)
            return TaskStatus.Failure;

        float dx = target.transform.position.x - transform.position.x;
        return dx * Facing2D.Get(transform) > 0f ? TaskStatus.Success : TaskStatus.Failure;
    }
}
#endif
