using System.Collections.Generic;


public enum NodeStatus { Success, Failure, Running }

/// <summary> Base class for every BT node. </summary>
public abstract class Node
{
    public abstract NodeStatus Tick();
}


/// <summary>
/// Sequence: runs children left-to-right.
/// Returns Failure on the first child that fails.
/// Returns Success only if all children succeed.
/// </summary>
public class Sequence : Node
{
    List<Node> children;

    public Sequence(params Node[] nodes)
    {
        children = new List<Node>(nodes);
    }

    public override NodeStatus Tick()
    {
        foreach (Node child in children)
        {
            NodeStatus status = child.Tick();
            if (status != NodeStatus.Success)
                return status;
        }
        return NodeStatus.Success;
    }
}

/// <summary>
/// Selector: runs children left-to-right.
/// Returns Success on the first child that succeeds.
/// Returns Failure only if all children fail.
/// </summary>
public class Selector : Node
{
    List<Node> children;

    public Selector(params Node[] nodes)
    {
        children = new List<Node>(nodes);
    }

    public override NodeStatus Tick()
    {
        foreach (Node child in children)
        {
            NodeStatus status = child.Tick();
            if (status != NodeStatus.Failure)
                return status;
        }
        return NodeStatus.Failure;
    }
}

// ── Decorators ────────────────────────────────────────────────────────────────

/// <summary> Inverts child result: Success becomes Failure and vice-versa. </summary>
public class Inverter : Node
{
    Node child;

    public Inverter(Node node)
    {
        child = node;
    }

    public override NodeStatus Tick()
    {
        NodeStatus status = child.Tick();
        if (status == NodeStatus.Success) return NodeStatus.Failure;
        if (status == NodeStatus.Failure) return NodeStatus.Success;
        return NodeStatus.Running;
    }
}

/// <summary> Repeats child until it fails. </summary>
public class RepeatUntilFail : Node
{
    Node child;

    public RepeatUntilFail(Node node)
    {
        child = node;
    }

    public override NodeStatus Tick()
    {
        NodeStatus status = child.Tick();
        if (status == NodeStatus.Failure) return NodeStatus.Success;
        return NodeStatus.Running;
    }
}


/// <summary> Wraps a condition delegate as a leaf node. </summary>
public class Condition : Node
{
    System.Func<bool> condition;

    public Condition(System.Func<bool> check)
    {
        condition = check;
    }

    public override NodeStatus Tick()
    {
        return condition() ? NodeStatus.Success : NodeStatus.Failure;
    }
}

/// <summary> Wraps an action delegate as a leaf node. </summary>
public class ActionNode : Node
{
    System.Func<NodeStatus> action;

    public ActionNode(System.Func<NodeStatus> act)
    {
        action = act;
    }

    public override NodeStatus Tick()
    {
        return action();
    }
}