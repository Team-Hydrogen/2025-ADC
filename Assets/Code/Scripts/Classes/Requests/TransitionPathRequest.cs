using UnityEngine;

public class TransitionPathRequest
{
    private float[] originPosition;
    private float[] originVelocity;

    private float[] destinationPosition;
    private float[] destinationVelocity;

    private float startTime;
    private float flightTime;
    
    /// <summary>
    /// Parameter constructor for TransitionPathRequest
    /// </summary>
    /// <param name="op">The original position</param>
    /// <param name="ov">The original velocity</param>
    /// <param name="dp">The destination position</param>
    /// <param name="dv">The destination velocity</param>
    /// <param name="st">The initial time</param>
    /// <param name="ft">The expected flight time</param>
    public TransitionPathRequest(float[] op, float[] ov, float[] dp, float[] dv, float st, float ft)
    {
        originPosition = op;
        originVelocity = ov;
        destinationPosition = dp;
        destinationVelocity = dv;
        startTime = st;
        flightTime = ft;
    }
    
    /// <summary>
    /// Converts required data for TransitionPathRequest into JSON format
    /// </summary>
    /// <param name="op">The original position</param>
    /// <param name="ov">The original velocity</param>
    /// <param name="dp">The destination position</param>
    /// <param name="dv">The destination velocity</param>
    /// <param name="st">The initial time</param>
    /// <param name="ft">The expected flight time</param>
    /// <returns>A JSON-formatted string with information</returns>
    public static string ToJson(Vector3 op, Vector3 ov, Vector3 dp, Vector3 dv, float st, float ft)
    {
        float[] originPositionPostData = { op.x, op.y, op.z };
        float[] originVelocityPostData = { ov.x, ov.y, ov.z };
        float[] destinationPositionPostData = { dp.x, dp.y, dp.z };
        float[] destinationVelocityPostData = { dv.x, dv.y, dv.z };

        TransitionPathRequest apiRequest = new TransitionPathRequest(
            originPositionPostData,
            originVelocityPostData,
            destinationPositionPostData,
            destinationVelocityPostData,
            st,
            ft
        );
        
        return JsonUtility.ToJson(apiRequest);
    }
}