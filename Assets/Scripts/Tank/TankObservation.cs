using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TankObservation : MonoBehaviour
{
    private const int NUM_OF_LASERS = 90;
    private Transform m_flag;

    private float gameboardLength;

    private float[] m_LidarData;
    private float x;
    private float z;
    private float direction_x;
    private float direction_z;
    private float distance_from_flag;

    private volatile bool updated = false;
    private readonly System.Threading.EventWaitHandle waitHandle = new System.Threading.AutoResetEvent(false);

    private Vector3 lastPosition;
    private Vector3 lastDirection;
    private Vector3 currentPosition;
    private Vector3 currentDirection;

    // Start is called before the first frame update
    void Start()
    {
        m_LidarData = new float[NUM_OF_LASERS];
        lastPosition = transform.position;

        // Game board
        float width = 100;
        float height = 100;
        gameboardLength = (float)Math.Sqrt(width * width + height * height);
        //Debug.Log($"Gameboard Length: {gameboardLength}");
    }

    //bool logged;
    // Update is called once per frame
    void Update()
    {
        lidar();
        position();
        direction();
        distanceFromFlag();

        if (!updated)
        {
            updatePostion();
            updated = true;
            waitHandle.Set();
        }
        
        //travelled();
    }

    public void SetFlag(Transform flag)
    {
        m_flag = flag;
    }

    public void lidar()
    {
        for (int i = 0; i < NUM_OF_LASERS; i++)
        {
            float angle = -Mathf.Lerp(0, 360, (i / (float)(NUM_OF_LASERS)));

            Vector3 fwd = transform.TransformDirection(Quaternion.Euler(0, angle, 0) * Vector3.forward);
            RaycastHit hit;
            float dist = 0;

            if (Physics.Raycast(transform.position, fwd, out hit, gameboardLength))
            {
                dist = hit.distance;
                //dist = hit.distance + Random.Range(-gameboardLength, gameboardLength);
                //dist = Mathf.Clamp(dist, 0, gameboardLength);

                // The line
                Debug.DrawLine(transform.position, hit.point, Color.green);
                // The X
                Debug.DrawLine(hit.point - Vector3.up * 0.3f, hit.point + Vector3.up * 0.3f, Color.red, 0, false);
                Debug.DrawLine(hit.point - Vector3.left * 0.3f, hit.point + Vector3.left * 0.3f, Color.red, 0, false);
                Debug.DrawLine(hit.point - Vector3.forward * 0.3f, hit.point + Vector3.forward * 0.3f, Color.red, 0, false);
            }
            else
            {
                Debug.Log("failed to hit anything");
            }

            m_LidarData[i] = dist;
        }
    }

    void RenderSlice(float horizontalAngle, out LaserSliceData outSlice)
    {
        int Channels = 64;
        float MaximalVerticalFOV = +0.2f;
        float MinimalVerticalFOV = -24.9f;
        float MeasurementRange = 120f;
        float MeasurementAccuracy = 0.02f;

        LaserData[] lasers = new LaserData[Channels];

        for (int i = 0; i < Channels; i++)
        {
            

            RaycastHit hit;

            float dist;
            float verticalAngel = -Mathf.Lerp(MaximalVerticalFOV, MinimalVerticalFOV, (i / (float)(NUM_OF_LASERS - 1)));
            //Debug.LogFormat("verticalAngel : {0}, Vector: {1}", verticalAngel, Quaternion.Euler(verticalAngel, 0, 0) * Vector3.forward);

            Vector3 fwd = transform.TransformDirection(Quaternion.Euler(verticalAngel, horizontalAngle, 0) * Vector3.forward);
            if (Physics.Raycast(transform.position, fwd, out hit, MeasurementRange))
            {
                dist = hit.distance + UnityEngine.Random.Range(-MeasurementAccuracy, MeasurementAccuracy);
                dist = Mathf.Clamp(dist, 0, MeasurementRange);

                Debug.DrawLine(transform.position, hit.point, Color.green);
                Debug.DrawLine(hit.point - Vector3.up * 0.3f, hit.point + Vector3.up * 0.3f, Color.red, 0, false);
                Debug.DrawLine(hit.point - Vector3.left * 0.3f, hit.point + Vector3.left * 0.3f, Color.red, 0, false);
                Debug.DrawLine(hit.point - Vector3.forward * 0.3f, hit.point + Vector3.forward * 0.3f, Color.red, 0, false);
            }
            else
            {
                dist = MeasurementRange;
                Debug.DrawRay(transform.position, fwd, Color.gray);
            }

            //Debug.LogFormat(dist.ToString());

            lasers[i] = new LaserData()
            {
                distance = dist,
            };
        }

        LaserSliceData laserSliceData = new LaserSliceData()
        {
            RotationalPosition = horizontalAngle,
            Timestamp = Time.time,
            Lasers = lasers,
        };

        outSlice = laserSliceData;
    }

    public struct LaserData
    {
        public float distance;
        public float intensity;
    }

    public struct LaserSliceData
    {
        public float RotationalPosition;
        public LaserData[] Lasers;
        public float Timestamp;
    }

    private void position()
    {
        x = transform.position.x;
        z = transform.position.z;
    }
    private void direction()
    {
        direction_x = transform.forward.x;
        direction_z = transform.forward.z;
    }
    public void distanceFromFlag()
    {
        distance_from_flag = Vector3.Distance(transform.position, m_flag.position);
    }

    public void updatePostion()
    {
        currentPosition = transform.position;
        currentDirection = transform.forward;
    }

    public float[] getLidarData() { return m_LidarData; }
    public float getX() { return x; }
    public float getY() { return z; }
    public float getDirectionX() { return direction_x; }
    public float getDirectionY() { return direction_z; }
    public float getDistanceToFlag() { return distance_from_flag; }

    public Tuple<float, float> getOdometry()
    {
        updated = false;
        waitHandle.WaitOne();

        float angleTurned = Vector3.SignedAngle(lastDirection, currentDirection, Vector3.up);
        float distanceTravelled = Vector3.Distance(lastPosition, currentPosition);

        //Quaternion rotation = Quaternion.Euler(0, angleTurned, 0);
        //Vector3 estimatedDirection = rotation * lastDirection;
        //Debug.Log($"{lastDirection} -{angleTurned}-> {estimatedDirection} = {currentDirection}");
        //total += angleTurned;
        //Debug.Log(total);

        lastDirection = currentDirection;
        lastPosition = currentPosition;

        return Tuple.Create(distanceTravelled, angleTurned);
    }
}
