using UnityEngine;

public class GoalTrigger : MonoBehaviour
{
    [SerializeField] private SoccerAgent agent;
    [SerializeField] private bool isOpponentGoal = true;
    [SerializeField] private string ballTag = "Ball";

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(ballTag))
        {
            return;
        }

        if (agent == null)
        {
            return;
        }

        if (isOpponentGoal)
        {
            agent.NotifyScored();
        }
        else
        {
            agent.NotifyConceded();
        }
    }
}
