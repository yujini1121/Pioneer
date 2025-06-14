using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 실제로 어떤 행위를 하는 노드
/// </summary>
public class ActionNode : INode
{
    Func<INode.ENodeState> _onUpdate = null;

    public ActionNode(Func<INode.ENodeState> onUpdate)
    {
        _onUpdate = onUpdate;
    }

    public INode.ENodeState Evaluate() => _onUpdate?.Invoke() ?? INode.ENodeState.Failure;
}
