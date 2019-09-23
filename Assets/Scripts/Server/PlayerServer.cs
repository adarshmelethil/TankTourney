using System;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Text.RegularExpressions;


public class PlayerServer
{
    private const int CONTROL_PORT = 26000;
    private const int OBSERVATION_PORT = 27000;

    // Control
    private TankMovement m_Movement;
    private TankObservation m_Observation;
    private bool m_running;

    // Unique identifier (player number)
    private int m_portOffset;

    UdpClient m_ControllerClient;
    UdpClient m_ObservationClient;

    // Game Mode
    public bool DebugMode = true;


    public void Run(int portOffset, TankMovement mv, TankObservation obv)
    {
        m_Movement = mv;
        m_Observation = obv;
        m_running = true;
        m_portOffset = portOffset;

        // Control thread
        ThreadStart controlThreadDelegate = new ThreadStart(StartControlListener);
        Thread controlThread = new Thread(controlThreadDelegate);
        controlThread.Start();
        
        // Observation thread
        //ThreadStart observationThreadDelegate = new ThreadStart(StartObservationListener);
        //Thread observationThread = new Thread(observationThreadDelegate);
        //observationThread.Start();
    }

    //void StartObservationListener()
    //{
    //    m_ObservationClient = new UdpClient(OBSERVATION_PORT + m_portOffset);
    //    IPEndPoint broadcastEndPoint = new IPEndPoint(IPAddress.Broadcast, OBSERVATION_PORT + m_portOffset);

    //    Debug.Log($"Waiting for broadcast: {OBSERVATION_PORT + m_portOffset}");
    //    try
    //    {
            

    //        while (m_running)
    //        {
    //            // connect to client
    //            byte[] bytes = m_ObservationClient.Receive(ref broadcastEndPoint);
    //            string obvStr = Encoding.UTF8.GetString(bytes, 0, bytes.Length);
    //            Debug.Log($"{obvStr}");

    //            string obvString = getObservation();
    //            Debug.Log($"Sending observation: {obvString}");

    //            // send observation
    //            byte[] sendbuf = Encoding.ASCII.GetBytes(obvString);
    //            m_ObservationClient.Send(sendbuf, sendbuf.Length, broadcastEndPoint);
    //        }
    //    }
    //    catch (SocketException e)
    //    {
    //        Debug.Log(e);
    //    }
    //    finally
    //    {
    //        m_ObservationClient.Close();
    //    }
    //}
    void StartControlListener()
    {
        m_ControllerClient = new UdpClient(CONTROL_PORT + m_portOffset);        
        IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Any, CONTROL_PORT + m_portOffset);

        Regex commandRegex = new Regex(@"^[-+]?[0-9]*\.?[0-9]+,[-+]?[0-9]*\.?[0-9]+,[-+]?[0-9]*\.?[0-9]+$");

        //Debug.Log($"Waiting for connection: {CONTROL_PORT + m_portOffset}");
        try
        {
            while (m_running)
            {
                byte[] bytes = m_ControllerClient.Receive(ref ipEndPoint);
                string commandString = Encoding.UTF8.GetString(bytes, 0, bytes.Length);

                Match commandMatch = commandRegex.Match(commandString);
                if (commandMatch.Success)
                {
                    // Got commands
                    //Debug.Log($"{ipEndPoint}: {commandString}");
                    setCommand(commandString);
                }
                // Send Observation
                string obvString = getObservation();
                // Debug.Log($"Sending observation: {obvString}");
                byte[] sendbuf = Encoding.ASCII.GetBytes(obvString);
                m_ControllerClient.Send(sendbuf, sendbuf.Length, ipEndPoint);
            }
        }
        catch (SocketException e)
        {
            Debug.Log(e);
        }
        finally
        {
            m_ControllerClient.Close();
        }
    }
    private string getObservation()
    {
        Observation obv;
        var odom = m_Observation.getOdometry();
        if (DebugMode) {
            obv = new DebugObservation(
                m_Observation.getDistanceToFlag(),
                m_Observation.getLidarData(),
                odom.Item1,
                odom.Item2,
                new Position(m_Observation.getX(), m_Observation.getY()),
                new Direction(m_Observation.getDirectionX(), m_Observation.getDirectionY())
            );
        }
        else
        {
            obv = new Observation(
                m_Observation.getDistanceToFlag(),
                m_Observation.getLidarData(),
                odom.Item1,
                odom.Item2
            );
        }
        
        return JsonUtility.ToJson(obv);
    }
    

    void setCommand(string commandString)
    {
        string[] commands = commandString.Split(',');

        // Read movement
        if (commands.Length > 0)
        {
            float movement_val;
            if (!float.TryParse(commands[0], out movement_val))
            {
                //m_Movement.m_MovementInputValue = 0f;
                Debug.Log($"Failed to parse {commands[0]}");
            }
            else
            {
                if (Math.Abs(movement_val) > 0)
                {
                    m_Movement.m_MovementInputValue = movement_val;
                }
            }
        }

        // Read turn
        if (commands.Length > 1)
        {
            float turn_val = 0;
            if (!float.TryParse(commands[1], out turn_val))
            {
                //m_Movement.m_TurnInputValue = 0f;
                UnityEngine.Debug.Log($"Failed to parse {commands[1]}");
            }
            else
            {
                if (Math.Abs(turn_val) > 0) {
                    m_Movement.m_TurnInputValue = turn_val;
                }
            }
        }
    }

    public void Stop()
    {
        m_running = false;

        if (m_ObservationClient != null)
        {
            m_ObservationClient.Close();
        }
        if (m_ControllerClient != null)
        {
            m_ControllerClient.Close();
        }
    }

    [Serializable]
    class Observation
    {
        public float Flag;
        public float[] Lidar;
        public float DistanceTravelled;
        public float AngleTurned;
        public Observation(float flag, float[] lidar, float travelled, float turned)
        {
            Flag = flag;
            Lidar = lidar;
            DistanceTravelled = travelled;
            AngleTurned = turned;
        }
    }
    [Serializable]
    class DebugObservation : Observation
    {
        public Position Position;
        public Direction Direction;
        public DebugObservation(
            float flag, float[] lidar, float travelled, float turned,
            Position position, Direction direction
        ) : base (flag, lidar, travelled, turned)
        {
            Position = position;
            Direction = direction;
        }
    }
    [Serializable]
    class Position
    {
        public float X;
        public float Y;
        public Position(float x, float y)
        {
            X = x;
            Y = y;
        }
    }
    [Serializable]
    class Direction
    {
        public float X;
        public float Y;
        public Direction(float x, float y)
        {
            X = x;
            Y = y;
        }
    }
}
