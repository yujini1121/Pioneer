using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 자식 노드를 왼쪽에서 오른쪽으로 Failure 상태가 나올때까지 진행
/// Running 상태일땐 Running 상태 유지를 위해 Running 반환 
/// </summary>
public class SequenceNode : INode
{
    List<INode> _childs;

    public SequenceNode(List<INode> childs)
    {
        _childs = childs;
    }

    public INode.ENodeState Evaluate()
    {
        if (_childs == null || _childs.Count == 0)
            return INode.ENodeState.Failure;

        foreach(var child in _childs)
        {
            switch(child.Evaluate())
            {
                case INode.ENodeState.Running:
                    return INode.ENodeState.Running;
                case INode.ENodeState.Success:
                    continue;
                case INode.ENodeState.Failure:
                    return INode.ENodeState.Failure;
            }
        }

        return INode.ENodeState.Success;
    }
}
