using System;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using Newtonsoft.Json;


public class PlayerServer
{
    private const int CONTROL_PORT = 26000;
    private const int OBSERVATION_PORT = 27000;
    private const long DISCONNECT_TIME = 10;

    // Control
    private TankMovement m_Movement;
    private TankObservation m_Observation;
    private TankShooting m_Shooting;
    private GenerateMaze m_GenerateMaze;

    private bool m_running;
    private bool m_isPaused;

    // Unique identifier (player number)
    private int m_portOffset;

    UdpClient m_ControllerClient;
    private long m_commandRecv;

    //UdpClient m_ObservationClient;
    IPEndPoint m_ipEndPoint;
    private bool m_playerConnected;

    // Game Mode
    public bool DebugMode = true;

    public void Run(bool debug, int portOffset, TankMovement mv, TankObservation obv, TankShooting shoot, GenerateMaze generateMaze)
    {
        DebugMode = debug;
        m_Movement = mv;
        m_Observation = obv;
        m_Shooting = shoot;

        m_GenerateMaze = generateMaze;
        m_running = true;
        m_portOffset = portOffset;

        m_ControllerClient = new UdpClient(CONTROL_PORT + m_portOffset);
        //m_ObservationClient = new UdpClient(OBSERVATION_PORT + m_portOffset);
        m_ipEndPoint = new IPEndPoint(IPAddress.Any, OBSERVATION_PORT + m_portOffset);
        var debugString = DebugMode ? "Debug" : "";
        var startString = $"Starting {debugString} Server: {m_ipEndPoint.Address}";
        Debug.Log(startString);

        // Control thread
        ThreadStart controlThreadDelegate = new ThreadStart(ControlListener);
        Thread controlThread = new Thread(controlThreadDelegate);
        controlThread.Start();

        // Obervation thread
        ThreadStart observationThreadDelegate = new ThreadStart(ObservationListener);
        Thread observationThread = new Thread(observationThreadDelegate);
        observationThread.Start();
    }
    public bool playerConnected()
    {
        return m_playerConnected;
    }
    void ObservationListener()
    {
        try
        {
            while (m_running)
            {
                if (m_playerConnected)
                {
                    if (m_ControllerClient == null) break;
                    var now = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
                    if (now - m_commandRecv > DISCONNECT_TIME)
                    {
                        m_playerConnected = false;
                        continue;
                    }
                    // Send Observation
                    string obvString = getObservation();
                    byte[] sendbuf = Encoding.ASCII.GetBytes(obvString);
                    
                    m_ControllerClient.Send(sendbuf, sendbuf.Length, m_ipEndPoint.Address.ToString(), OBSERVATION_PORT+ m_portOffset);
                    //Debug.Log($"Sending obv: {m_ipEndPoint.Address.ToString()}:{OBSERVATION_PORT+ m_portOffset} {sendbuf.Length}");
                }
            }
        }
        catch (SocketException e)
        {
            Debug.Log(e);
            if (m_running)
            {
                if (m_ControllerClient != null)
                {
                    m_ControllerClient.Close();
                }
                Debug.LogWarning($"Restarting Obv server {m_portOffset}");
                ThreadStart observationThreadDelegate = new ThreadStart(ObservationListener);
                Thread observationThread = new Thread(observationThreadDelegate);
                observationThread.Start();
            }
        }
        finally
        {
            Debug.Log($"Closed UdpClient: '{CONTROL_PORT + m_portOffset}'");

            if (m_ControllerClient != null)
            {
                m_ControllerClient.Close();
            }
        }
    }
    void ControlListener()
    {   
        try
        {
            while (m_running)
            {
                byte[] bytes = m_ControllerClient.Receive(ref m_ipEndPoint);
                m_playerConnected = true;
                m_commandRecv = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();

                string commandString = Encoding.UTF8.GetString(bytes, 0, bytes.Length);
                //Debug.Log($"{CONTROL_PORT + m_portOffset}: {commandString}");
                //Debug.Log($"After: {m_ipEndPoint.Address}");
                setCommand(commandString);
            }
        }
        catch (SocketException e)
        {
            Debug.Log(e);
            if (m_running)
            {
                if (m_ControllerClient != null)
                {
                    m_ControllerClient.Close();
                }
                Debug.LogWarning($"Restarting Control server {m_portOffset}");
                ThreadStart controlThreadDelegate = new ThreadStart(ControlListener);
                Thread controlThread = new Thread(controlThreadDelegate);
                controlThread.Start();
            }
        }
        finally
        {
            Debug.Log($"Closed UdpClient: '{CONTROL_PORT + m_portOffset}'");

            if (m_ControllerClient != null)
            {
                m_ControllerClient.Close();
            }
        }
    }

    private string getObservation()
    {
        Observation obv;
        var odom = m_Observation.getOdometry();
        var pos = m_Observation.getPosition();
        if (DebugMode) {
            obv = new DebugObservation(
                m_Observation.getDistanceToFlag(),
                m_Observation.getLidarData(),
                odom.Item1,
                odom.Item2,
                m_isPaused,
                pos,
                m_GenerateMaze.getCloseWallPositions(pos.Item1, pos.Item2, 10),
                m_GenerateMaze.getEdgeWallPositions()
            );
        }
        else
        {
            obv = new Observation(
                m_Observation.getDistanceToFlag(),
                m_Observation.getLidarData(),
                odom.Item1,
                odom.Item2,
                m_isPaused
            );
        }

        //new JsonSerializer();
        //return JsonUtility.ToJson(obv);
        return JsonConvert.SerializeObject(obv, new DecimalFormatConverter());
    }
    public class DecimalFormatConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(decimal));
        }

        public override void WriteJson(JsonWriter writer, object value,
                                       JsonSerializer serializer)
        {
            writer.WriteValue(string.Format("{0:N2}", value));
        }

        public override bool CanRead
        {
            get { return false; }
        }

        public override object ReadJson(JsonReader reader, Type objectType,
                                     object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
    private float clampVal(float val, float minVal, float maxval)
    {
        return Math.Min(maxval, Math.Max(minVal, val));
    }

    public class CommandData
    {
        public float fwd { get; set; }
        public float turn { get; set; }
        public float fire { get; set; }
    }
    void setCommand(string commandString)
    {
        var cmdData = (CommandData)Newtonsoft.Json.JsonConvert.DeserializeObject(commandString, typeof(CommandData));

        m_Movement.m_MovementInputValue = clampVal(cmdData.fwd, -1, 1);
        m_Movement.m_TurnInputValue = clampVal(cmdData.turn, -1, 1);
        m_Shooting.BotFire(clampVal(cmdData.fire, 0, 1));
    }

    void Pause() { m_isPaused = true; }
    void Resume() { m_isPaused = false; }
    public void Stop() { Shutdown(); }
    public void Shutdown()
    {
        if (m_ControllerClient != null) m_ControllerClient.Close();
        //if (m_ObservationClient != null)
        //    m_ObservationClient.Close();
        m_running = false;
    }

    [Serializable]
    class Observation
    {
        public float F; // Flag
        public float[] L; // Lidar
        public float DT; // DistanceTravelled
        public float AT; // AngleTurned
        public bool P; // Paused
        public Observation(float flag, float[] lidar, float travelled, float turned, bool paused)
        {
            F = (float)Math.Round(flag, 2);

            L = new float[lidar.Length];
            for (int i = 0; i < lidar.Length; i++)
                L[i] = (float)Math.Round(lidar[i], 2);

            DT = (float)Math.Round(travelled, 2);
            AT = (float)Math.Round(turned, 2);
            P = paused;
        }
    }

    [Serializable]
    class DebugObservation : Observation
    {
        public TupleValue Pos; // Position
        public TupleValue[] Obs; // Obstacles
        public TupleValue[] Edges; // Edge Walls

        public DebugObservation(
            float flag, float[] lidar, float travelled, float turned, bool paused,
            Tuple<float, float, float> position, List<Tuple<float, float, float>> obstacles,
            List<Tuple<float, float>> edgewalls
        ) : base (flag, lidar, travelled, turned, paused)
        {
            Pos = new TupleValue(position.Item1, position.Item2, position.Item3);

            Obs = new TupleValue[obstacles.Count];
            for(int i=0; i < obstacles.Count; i++)
            {
                Obs[i] = new TupleValue(
                    obstacles[i].Item1,
                    obstacles[i].Item2,
                    obstacles[i].Item3);
            }

            Edges = new TupleValue[edgewalls.Count];
            for(int i=0; i < edgewalls.Count; i++)
            {
                Edges[i] = new TupleValue(
                    edgewalls[i].Item1,
                    edgewalls[i].Item2,
                    0);
            }
        }
    }

    [Serializable]
    class TupleValue
    {
        public float X;
        public float Y;
        public float R;
        public TupleValue(float x, float y, float r)
        {
            X = (float)Math.Round(x, 2);
            Y = (float)Math.Round(y, 2);
            R = (float)Math.Round(r, 2);
        }
    }

}
