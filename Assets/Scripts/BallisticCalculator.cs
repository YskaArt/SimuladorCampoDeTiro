using UnityEngine;

public static class BallisticCalculator
{
    private const float AirDensity = 1.225f;

    public static Vector3 CalculateDragForce(Vector3 velocity, float dragCoefficient, float area)
    {
        float speed = velocity.magnitude;
        if (speed <= 0f) return Vector3.zero;

        // drag vector: -0.5 * rho * Cd * A * v^2 * v_normalized
        return -0.5f * AirDensity * dragCoefficient * area * speed * velocity;
    }
}
