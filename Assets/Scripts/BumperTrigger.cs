using UnityEngine;

public class BumperTrigger : MonoBehaviour
{
    [SerializeField] private SoccerAgent agent;
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

        agent.SetBallInBumper(true);
        agent.RegisterBallTouch();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(ballTag))
        {
            return;
        }

        if (agent == null)
        {
            return;
        }

        agent.SetBallInBumper(false);
    }

    private void OnDisable()
    {
        if (agent != null)
        {
            agent.SetBallInBumper(false);
        }
    }
}
