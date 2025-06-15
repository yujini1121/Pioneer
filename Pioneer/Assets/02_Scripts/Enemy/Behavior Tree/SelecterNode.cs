using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 자식 노드 중 Success나 Running 상태를 가진 노드가 발생시 그 노드까지 진행하고 멈춤
/// </summary>
public class SelecterNode : INode
{
    List<INode> _childs;

    public SelecterNode(List<INode> childs)
    {
        _childs = childs;
    }

    public INode.ENodeState Evaluate()
    {
        if(_childs == null)
            return INode.ENodeState.Failure;

        foreach(var child in _childs)
        {
            switch(child.Evaluate())
            {
                case INode.ENodeState.Running:
                    return INode.ENodeState.Running;
                case INode.ENodeState.Success:
                    return INode.ENodeState.Success;
            }
        }

        return INode.ENodeState.Failure;
    }
}
