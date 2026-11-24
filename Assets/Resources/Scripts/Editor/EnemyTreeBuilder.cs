#if GRAPH_DESIGNER
using System;
using System.Collections.Generic;
using System.Reflection;
using Opsive.BehaviorDesigner.Runtime.Tasks;
using Opsive.BehaviorDesigner.Runtime.Tasks.Actions;
using Opsive.BehaviorDesigner.Runtime.Tasks.Actions.AnimatorTasks;
using Opsive.BehaviorDesigner.Runtime.Tasks.Actions.Time;
using Opsive.BehaviorDesigner.Runtime.Tasks.Composites;
using Opsive.BehaviorDesigner.Runtime.Tasks.Conditionals;
using Opsive.BehaviorDesigner.Runtime.Tasks.Conditionals.Math;
using Opsive.BehaviorDesigner.Runtime.Tasks.Decorators;
using Opsive.BehaviorDesigner.Runtime.Tasks.Events;
using Opsive.GraphDesigner.Runtime;
using Opsive.GraphDesigner.Runtime.Variables;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using BTree = Opsive.BehaviorDesigner.Runtime.BehaviorTree;
using Scope = Opsive.GraphDesigner.Runtime.Variables.SharedVariable.SharingScope;

// Tools > Enemy AI > Build Behavior Tree: dựng cây hành vi enemy (idle / patrol / chase / attack) trên GameObject đang chọn.
// Ghi đè cây cũ trên object đó (có hỏi trước). Khoảng cách chỉnh trong tab Variables của Behavior Designer:
// ChaseRange, AttackRange, PatrolDistance, AttackAnimation.
public static class EnemyTreeBuilder
{
    private class N
    {
        public ITreeLogicNode Node;
        public string Name;
        public N[] Kids;
        public ushort Index, Parent = ushort.MaxValue, Sibling = ushort.MaxValue;
        public Vector2 Pos;
    }

    [MenuItem("Tools/Enemy AI/Build Behavior Tree")]
    private static void Build()
    {
        var go = Selection.activeGameObject;
        if (go == null)
        {
            EditorUtility.DisplayDialog("Enemy AI", "Chọn GameObject enemy trong Hierarchy trước.", "OK");
            return;
        }

        var tree = go.GetComponent<BTree>();
        if (tree == null)
            tree = Undo.AddComponent<Opsive.BehaviorDesigner.Runtime.Wrappers.BehaviorTree>(go);
        else if (tree.LogicNodes != null && tree.LogicNodes.Length > 0
                 && !EditorUtility.DisplayDialog("Enemy AI", $"'{go.name}' đã có cây hành vi. Ghi đè?", "Ghi đè", "Hủy"))
            return;
        Undo.RecordObject(tree, "Build Enemy Behavior Tree");

        var player = UnityEngine.Object.FindAnyObjectByType<Player>(FindObjectsInactive.Include);
        tree.SharedVariables = new SharedVariable[]
        {
            Var("Player", player != null ? player.gameObject : null),
            Var("ChaseRange", 3f),
            Var("AttackRange", 1f),
            Var("PatrolDistance", 4f),
            Var("AttackAnimation", "Attack"),
        };

        var root =
            Node(new Repeater(), "Repeat Forever",
                Node(new Selector(), "Selector",
                    Node(new Sequence { AbortType = ConditionalAbortType.LowerPriority }, "Attack",
                        Stack(Task(new IsTargetInFront2D(), ("m_Target", Ref<GameObject>("Player"))), "Player In Front"),
                        Stack(Task(new DistanceComparison(),
                            ("m_Operation", DistanceComparison.Operation.LessThan),
                            ("m_GameObject2", Ref<GameObject>("Player")),
                            ("m_Distance", Ref<float>("AttackRange"))), "Distance < AttackRange"),
                        Stack(Task(new Play(),
                            ("m_StateName", Ref<string>("AttackAnimation")),
                            ("m_WaitForCompletion", Lit(true))), "Play Attack")),
                    Node(new Sequence { AbortType = ConditionalAbortType.Both }, "Chase",
                        Stack(Task(new IsTargetInFront2D(), ("m_Target", Ref<GameObject>("Player"))), "Player In Front"),
                        Stack(Task(new DistanceComparison(),
                            ("m_Operation", DistanceComparison.Operation.LessThan),
                            ("m_GameObject2", Ref<GameObject>("Player")),
                            ("m_Distance", Ref<float>("ChaseRange"))), "Distance < ChaseRange"),
                        Stack(Task(new Chase2D(),
                            ("m_Target", Ref<GameObject>("Player")),
                            ("m_StopDistance", Ref<float>("AttackRange"))), "Chase")),
                    Node(new Sequence(), "Idle / Patrol",
                        Node(Task(new Wait(), ("m_Duration", Lit(1.5f))), "Idle"),
                        Stack(Task(new Patrol2D(), ("m_Distance", Ref<float>("PatrolDistance"))), "Patrol"))));

        // Đánh số theo thứ tự duyệt trước (cha trước con, trái sang phải) và xếp vị trí cho editor.
        var order = new List<N>();
        float leafX = 0f;
        Layout(root, 0, order, ref leafX);

        var nodes = new ITreeLogicNode[order.Count];
        var props = new LogicNodeProperties[order.Count];
        for (int i = 0; i < order.Count; i++)
        {
            var n = order[i];
            n.Node.GetType().GetProperty("Index").SetValue(n.Node, n.Index);
            n.Node.ParentIndex = n.Parent;
            n.Node.SiblingIndex = n.Sibling;
            nodes[i] = n.Node;

            var stacked = n.Node as StackedTask;
            props[i] = new LogicNodeProperties(Guid.NewGuid(), n.Pos, 0f, new LogicNodeProperties.NodeData
            {
                ParentIndex = n.Parent,
                SiblingIndex = n.Sibling,
                IsParent = n.Kids.Length > 0,
                ContainedNodeTypes = stacked == null ? null : Array.ConvertAll(stacked.Tasks, t => t.GetType().FullName),
                ContainedNodeNames = stacked == null ? null : new[] { n.Name },
            }, n.Name, null);
        }

        tree.LogicNodes = nodes;
        tree.LogicNodeProperties = props;
        tree.EventNodes = new IEventNode[] { new Start { ConnectedIndex = 0 } };
        tree.EventNodeProperties = new[] { new NodeProperties(Guid.NewGuid(), new Vector2(root.Pos.x, -150f)) };
        tree.Serialize();

        EditorUtility.SetDirty(tree);
        EditorSceneManager.MarkSceneDirty(go.scene);
        Debug.Log($"Đã dựng cây hành vi enemy trên '{go.name}' ({order.Count} node)."
                  + (player == null ? " Không tìm thấy Player trong scene: gán biến Player bằng tay." : ""), go);
    }

    private static void Layout(N n, int depth, List<N> order, ref float leafX)
    {
        n.Index = (ushort)order.Count;
        order.Add(n);
        for (int i = 0; i < n.Kids.Length; i++)
        {
            n.Kids[i].Parent = n.Index;
            Layout(n.Kids[i], depth + 1, order, ref leafX);
        }
        for (int i = 0; i + 1 < n.Kids.Length; i++)
            n.Kids[i].Sibling = n.Kids[i + 1].Index;

        float x = n.Kids.Length == 0 ? leafX++ : (n.Kids[0].Pos.x + n.Kids[n.Kids.Length - 1].Pos.x) / 2f / 220f;
        n.Pos = new Vector2(x * 220f, depth * 150f);
    }

    private static N Node(object node, string name, params N[] kids)
    {
        return new N { Node = (ITreeLogicNode)node, Name = name, Kids = kids };
    }

    // Task thường (Action/Conditional) phải nằm trong StackedAction/StackedConditional, giống cách Behavior Designer lưu.
    private static N Stack(Task task, string name)
    {
        StackedTask stacked = task is Conditional ? new StackedConditional() : new StackedAction();
        stacked.Tasks = new[] { task };
        return Node(stacked, name);
    }

    // Gán field (kể cả protected) theo tên; mọi field SharedVariable còn null được tạo mới để task không bị NullReference.
    private static T Task<T>(T task, params (string field, object value)[] values)
    {
        for (var t = task.GetType(); t != null && t != typeof(object); t = t.BaseType)
        {
            foreach (var f in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                if (typeof(SharedVariable).IsAssignableFrom(f.FieldType) && !f.FieldType.IsAbstract && f.GetValue(task) == null)
                    f.SetValue(task, Activator.CreateInstance(f.FieldType));
            }
        }
        foreach (var (field, value) in values)
            FindField(task.GetType(), field).SetValue(task, value);
        return task;
    }

    private static FieldInfo FindField(Type type, string name)
    {
        for (var t = type; t != null; t = t.BaseType)
        {
            var f = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (f != null)
                return f;
        }
        throw new MissingFieldException(type.Name, name);
    }

    private static SharedVariable Var<T>(string name, T value)
    {
        return new SharedVariable<T> { Name = name, Scope = Scope.Graph, Value = value };
    }

    // Field của task trỏ tới biến Graph cùng tên.
    private static SharedVariable<T> Ref<T>(string name)
    {
        return new SharedVariable<T> { Name = name, Scope = Scope.Graph };
    }

    private static SharedVariable<T> Lit<T>(T value)
    {
        return new SharedVariable<T> { Value = value };
    }
}
#endif
