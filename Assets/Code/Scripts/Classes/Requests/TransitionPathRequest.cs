using UnityEngine;

public class TransitionPathRequest
{
    public float[] OriginPosition;
    public float[] OriginVelocity;

    public float[] DestinationPosition;

    public float StartTime;
    public float FlightTime;

    /// <summary>
    /// Parameter constructor for TransitionPathRequest
    /// </summary>
    /// <param name="originPosition">The original position</param>
    /// <param name="originVelocity">The original velocity</param>
    /// <param name="destinationPosition">The destination position</param>
    /// <param name="startTime">The initial time</param>
    /// <param name="flightTime">The expected flight time</param>
    public TransitionPathRequest(
        float[] originPosition, float[] originVelocity, float[] destinationPosition, float startTime, float flightTime)
    {
        OriginPosition = originPosition;
        OriginVelocity = originVelocity;
        DestinationPosition = destinationPosition;
        StartTime = startTime;
        FlightTime = flightTime;
    }

    /// <summary>
    /// Converts required data for TransitionPathRequest into JSON format
    /// </summary>
    /// <param name="originPosition">The original position</param>
    /// <param name="originVelocity">The original velocity</param>
    /// <param name="destinationPosition">The destination position</param>
    /// <param name="startTime">The initial time</param>
    /// <param name="flightTime">The expected flight time</param>
    /// <returns>A JSON-formatted string with information</returns>
    public static string ToJson(Vector3 originPosition, Vector3 originVelocity, Vector3 destinationPosition,
        float startTime, float flightTime)
    {
        float[] originPositionPostData = { originPosition.x, originPosition.y, originPosition.z };
        float[] originVelocityPostData = { originVelocity.x, originVelocity.y, originVelocity.z };
        float[] destinationPositionPostData = { destinationPosition.x, destinationPosition.y, destinationPosition.z };

        TransitionPathRequest apiRequest = new TransitionPathRequest(
            originPositionPostData,
            originVelocityPostData,
            destinationPositionPostData,
            startTime,
            flightTime
        );
        
        return JsonUtility.ToJson(apiRequest);
    }
}