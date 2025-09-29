using UnityEngine.AI;

public class CrawlerAI : EnemyBase, IBegin
{
    // 네브 메시 
    private NavMeshAgent agent;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();   
    }

    void Update()
    {
        
    }
}
