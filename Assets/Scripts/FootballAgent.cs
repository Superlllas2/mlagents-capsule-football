using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class SoccerAgent : Agent
{
    [Header("Scene References")]
    [SerializeField] private Rigidbody ballRigidbody;
    [SerializeField] private Transform opponentGoal;
    [SerializeField] private Transform ownGoal;

    [Header("Movement")]
    [SerializeField] private float moveForce = 18f;
    [SerializeField] private float turnTorque = 50f;

    [Header("Ball Control")]
    [SerializeField] private float kickImpulse = 10f;
    [SerializeField] private float dribbleForce = 2f;
    [SerializeField, Range(0f, 1f)] private float forwardDotThreshold = 0.2f;

    [Header("Rewards")]
    [SerializeField] private float stepPenalty = -0.0005f;
    [SerializeField] private float ballTouchReward = 0.02f;
    [SerializeField] private float progressRewardScale = 0.03f;

    private Rigidbody agentRigidbody;
    private Vector3 initialAgentPosition;
    private Quaternion initialAgentRotation;
    private Vector3 initialBallPosition;
    private Quaternion initialBallRotation;
    private Transform agentSpawn;
    private Transform ballSpawn;

    private Vector2 moveInput;
    private float turnInput;
    private float kickInput;
    private bool ballInBumper;
    private float previousBallDistance;

    private void Awake()
    {
        agentRigidbody = GetComponent<Rigidbody>();
        agentRigidbody.maxAngularVelocity = 20f;
        initialAgentPosition = transform.position;
        initialAgentRotation = transform.rotation;
        if (ballRigidbody != null)
        {
            initialBallPosition = ballRigidbody.transform.position;
            initialBallRotation = ballRigidbody.transform.rotation;
        }

        agentSpawn = transform;
        ballSpawn = ballRigidbody.transform;
    }

    private void FixedUpdate()
    {
        ApplyMovement();
        ApplyBallInteractions();
    }

    public override void OnEpisodeBegin()
    {
        ResetBody(agentRigidbody, agentSpawn != null ? agentSpawn.position : initialAgentPosition,
            agentSpawn != null ? agentSpawn.rotation : initialAgentRotation);

        if (ballRigidbody != null)
        {
            ResetBody(ballRigidbody, ballSpawn != null ? ballSpawn.position : initialBallPosition,
                ballSpawn != null ? ballSpawn.rotation : initialBallRotation);
            if (opponentGoal != null)
            {
                previousBallDistance = Vector3.Distance(ballRigidbody.position, opponentGoal.position);
            }
            else
            {
                previousBallDistance = 0f;
            }
        }
        else
        {
            previousBallDistance = 0f;
        }

        ballInBumper = false;
        moveInput = Vector2.zero;
        turnInput = 0f;
        kickInput = 0f;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        Vector3 velocity = agentRigidbody.linearVelocity;
        sensor.AddObservation(new Vector2(velocity.x, velocity.z));

        Vector3 forward = transform.forward;
        sensor.AddObservation(new Vector2(forward.x, forward.z));

        if (ballRigidbody != null)
        {
            Vector3 toBall = ballRigidbody.position - transform.position;
            Vector2 toBall2D = new Vector2(toBall.x, toBall.z);
            Vector2 ballDirNormalized = toBall2D.normalized;
            sensor.AddObservation(ballDirNormalized);
            sensor.AddObservation(SafeNormalize(toBall2D));
            sensor.AddObservation(toBall2D.magnitude);

            Vector3 ballVelocity = ballRigidbody.linearVelocity;
            sensor.AddObservation(new Vector2(ballVelocity.x, ballVelocity.z));

            AddGoalObservation(sensor, opponentGoal);
            AddGoalObservation(sensor, ownGoal);
        }
        else
        {
            sensor.AddObservation(Vector2.zero);
            sensor.AddObservation(0f);
            sensor.AddObservation(Vector2.zero);
            sensor.AddObservation(Vector2.zero);
            sensor.AddObservation(0f);
            sensor.AddObservation(Vector2.zero);
            sensor.AddObservation(0f);
        }

        sensor.AddObservation(Vector2.zero);
        sensor.AddObservation(0f);
        sensor.AddObservation(Vector2.zero);
        sensor.AddObservation(0f);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        var continuousActions = actions.ContinuousActions;
        moveInput = Vector2.ClampMagnitude(new Vector2(continuousActions[0], continuousActions[1]), 1f);
        turnInput = Mathf.Clamp(continuousActions[2], -1f, 1f);
        kickInput = Mathf.Clamp01(continuousActions[3]);

        AddReward(stepPenalty);

        if (ballRigidbody != null && opponentGoal != null)
        {
            float currentDistance = Vector3.Distance(ballRigidbody.position, opponentGoal.position);
            float delta = previousBallDistance - currentDistance;
            AddReward(progressRewardScale * delta);
            previousBallDistance = currentDistance;
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActions = actionsOut.ContinuousActions;
        float moveX = 0f;
        float moveZ = 0f;
        if (Input.GetKey(KeyCode.A)) moveX -= 1f;
        if (Input.GetKey(KeyCode.D)) moveX += 1f;
        if (Input.GetKey(KeyCode.S)) moveZ -= 1f;
        if (Input.GetKey(KeyCode.W)) moveZ += 1f;

        float turn = 0f;
        if (Input.GetKey(KeyCode.Q)) turn -= 1f;
        if (Input.GetKey(KeyCode.E)) turn += 1f;

        float kick = Input.GetKey(KeyCode.Space) ? 1f : 0f;

        continuousActions[0] = moveX;
        continuousActions[1] = moveZ;
        continuousActions[2] = turn;
        continuousActions[3] = kick;
    }

    public void SetBallInBumper(bool inBumper)
    {
        ballInBumper = inBumper;
    }

    public void RegisterBallTouch()
    {
        AddReward(ballTouchReward);
    }

    public void NotifyScored()
    {
        AddReward(1f);
        EndEpisode();
    }

    public void NotifyConceded()
    {
        AddReward(-1f);
        EndEpisode();
    }

    private void ApplyMovement()
    {
        Vector3 moveVector = new Vector3(moveInput.x, 0f, moveInput.y);
        Vector3 worldMove = transform.TransformDirection(moveVector);
        agentRigidbody.AddForce(worldMove * moveForce, ForceMode.Acceleration);
        agentRigidbody.AddTorque(Vector3.up * turnInput * turnTorque, ForceMode.Acceleration);
    }

    private void ApplyBallInteractions()
    {
        if (!ballInBumper || ballRigidbody == null || !IsBallInFront())
        {
            return;
        }

        if (dribbleForce > 0f)
        {
            ballRigidbody.AddForce(transform.forward * dribbleForce, ForceMode.Force);
        }

        if (kickInput > 0.01f)
        {
            ballRigidbody.AddForce(transform.forward * (kickImpulse * kickInput), ForceMode.Impulse);
        }
    }

    private bool IsBallInFront()
    {
        Vector3 toBall = ballRigidbody.position - transform.position;
        Vector3 flattened = new Vector3(toBall.x, 0f, toBall.z);
        if (flattened.sqrMagnitude < Mathf.Epsilon)
        {
            return false;
        }

        float dot = Vector3.Dot(transform.forward, flattened.normalized);
        return dot > forwardDotThreshold;
    }

    private void ResetBody(Rigidbody body, Vector3 position, Quaternion rotation)
    {
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        body.position = position;
        body.rotation = rotation;
    }
    
    private static Vector2 SafeNormalize(Vector2 v)
    {
        float mag = v.magnitude;
        return mag > 1e-6f ? (v / mag) : Vector2.zero;
    }


    private void AddGoalObservation(VectorSensor sensor, Transform goal)
    {
        if (goal == null)
        {
            sensor.AddObservation(Vector2.zero);
            sensor.AddObservation(0f);
            return;
        }

        Vector3 toGoal = goal.position - transform.position;
        Vector2 toGoal2D = new Vector2(toGoal.x, toGoal.z);
        sensor.AddObservation(SafeNormalize(toGoal2D));
        sensor.AddObservation(toGoal2D.magnitude);
    }
}
