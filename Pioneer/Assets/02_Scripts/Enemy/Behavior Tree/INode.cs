public interface INode
{
    // 노드 상태
    public enum ENodeState
    {
        Running,
        Success,
        Failure
    }

    // 노드 상태 반환
    public ENodeState Evaluate();
}