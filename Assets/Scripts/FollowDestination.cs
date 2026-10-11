using UnityEngine;
public class FollowDestination : MonoBehaviour
{
    private UnityEngine.AI.NavMeshAgent ThisAgent = null;
    public Transform Destination = null;
    void Awake()
    {
        ThisAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
    }
    void Update()
    {
        ThisAgent.SetDestination(Destination.position);
    }
}