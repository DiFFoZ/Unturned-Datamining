using System;

namespace SDG.Unturned;

/// <summary>
/// Thanks to Glenn Fiedler for this RK4 implementation article:
/// https://gafferongames.com/post/integration_basics/
/// </summary>
[Serializable]
public struct Rk4Spring
{
    private struct Rk4Derivative
    {
        public float velocity;

        public float acceleration;
    }

    public float currentPosition;

    public float targetPosition;

    /// <summary>
    /// Higher values return to the target position faster.
    /// </summary>
    public float stiffness;

    /// <summary>
    /// Higher values reduce bounciness and settle at the target position faster.
    /// e.g. a value of zero will bounce back and forth for a long time (indefinitely?)
    /// </summary>
    public float damping;

    private float currentVelocity;

    /// <summary>
    /// At low framerate deltaTime can be so high the spring explodes unless we use a fixed timestep.
    /// </summary>
    internal const float MAX_TIMESTEP = 0.05f;

    public Rk4Spring(float stiffness, float damping)
    {
        currentPosition = 0f;
        targetPosition = 0f;
        this.stiffness = stiffness;
        this.damping = damping;
        currentVelocity = 0f;
    }

    public void Update(float deltaTime)
    {
        while (deltaTime > 0.05f)
        {
            PrivateUpdate(0.05f);
            deltaTime -= 0.05f;
        }
        if (deltaTime > 0f)
        {
            PrivateUpdate(deltaTime);
        }
    }

    private void PrivateUpdate(float deltaTime)
    {
        Rk4Derivative initialDerivative = Evaluate(0f, default(Rk4Derivative));
        Rk4Derivative initialDerivative2 = Evaluate(deltaTime * 0.5f, initialDerivative);
        Rk4Derivative initialDerivative3 = Evaluate(deltaTime * 0.5f, initialDerivative2);
        Rk4Derivative rk4Derivative = Evaluate(deltaTime, initialDerivative3);
        float num = 1f / 6f * (initialDerivative.velocity + 2f * (initialDerivative2.velocity + initialDerivative3.velocity) + rk4Derivative.velocity);
        float num2 = 1f / 6f * (initialDerivative.acceleration + 2f * (initialDerivative2.acceleration + initialDerivative3.acceleration) + rk4Derivative.acceleration);
        currentPosition += num * deltaTime;
        currentVelocity += num2 * deltaTime;
    }

    private Rk4Derivative Evaluate(float deltaTime, Rk4Derivative initialDerivative)
    {
        float num = currentPosition + initialDerivative.velocity * deltaTime;
        Rk4Derivative result = default(Rk4Derivative);
        float num2 = (result.velocity = currentVelocity + initialDerivative.acceleration * deltaTime);
        result.acceleration = stiffness * (targetPosition - num) - damping * num2;
        return result;
    }
}
