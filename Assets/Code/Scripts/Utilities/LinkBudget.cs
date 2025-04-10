using UnityEngine;

public static class LinkBudget
{
    private const float Pt = 10f;
    private const float Gt = 9f;
    private const float Losses = 19.43f;
    private const float nR = 0.55f;
    private const float Λ = 0.136363636f;
    private const float kb = -228.6f;
    private const float TS = 222f;
        
    private const float MaximumBn = 10_000f;
    
    /// <summary>
    /// Calculates the link budget
    /// </summary>
    /// <param name="dr">Ground station antenna diameter in meters (m)</param>
    /// <param name="R">Slant range in kilometers (km)</param>
    public static float CalculateBn(float dr, float R)
    {
        float Bn = 0.0f;
        return Mathf.Min(Bn, MaximumBn);
    }
}