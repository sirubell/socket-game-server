using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
class Server
{
    static public List<PlayerBlock> pbs = new List<PlayerBlock>();
    static public List<PlatformBlock> pfs = new List<PlatformBlock>();
    static long ms;
    public const long tick = 5;
    static bool gameStart;
    static string winner = String.Empty;
    public static void Main()
    {
        StartServer();

        while (true)
        {
            Renew();

            long prevTime = GetCurrentTimeMS();

            while (true)
            {
                long currentTime = GetCurrentTimeMS();
                if (currentTime >= prevTime + tick)
                {
                    prevTime = currentTime;

                    NextTick();

                    Console.WriteLine(GetEnvironmentString());
                }

            }
        }
        

        

        
    }

    public static Thread StartServer()
    {
        Thread t = new Thread(() => StartServerThread());
        t.Start();
        return t;
    }

    public static void StartServerThread()
    {
        Int32 port = 12345;
        int counter = 0;
        IPAddress localAddr = IPAddress.Parse("127.0.0.1");
        TcpListener server = new TcpListener(localAddr, port);
        server.Start();
        Console.WriteLine("Server start!");

        try
        {
            while (true)
            {
                counter += 1;
                TcpClient client = server.AcceptTcpClient();

                StartClientThread(client, "Player " + Convert.ToString(counter));
                Console.WriteLine($"Current threads count: {Process.GetCurrentProcess().Threads.Count}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
        finally
        {
            server.Stop();
        }
    }

    static public Thread StartClientThread(TcpClient client, string name)
    {
        Thread t = new Thread(() => KeepListening(client, name));
        t.Start();
        return t;
    }

    private static void KeepListening(TcpClient client, string name)
    {
        string clientIp = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
        string info = $"IP: {clientIp}, name: {name}";
        Console.WriteLine($"Client connected with {info}");

        pbs.Add(new PlayerBlock(300, 100, 50, 50, name));

        try
        {
            while (client.Connected)
            {
                NetworkStream stream = client.GetStream();
                byte[] buffer = new byte[2048];
                int bytesRead;
                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {

                    string msg = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    Console.WriteLine($"Receive {msg} from client: {info}");

                    PlayerBlock player = pbs.Find((PlayerBlock pb) => { return pb.name == name; });
                    player.ChangeDirection(msg);

                    msg = name + "\n" + msg;
                    // msg = msg.ToUpper();

                    stream.Write(Encoding.ASCII.GetBytes(msg), 0, bytesRead);
                    Console.WriteLine($"Send {msg} to client: {info}");
                }

                stream.Flush();
                stream.Close();
            }

        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
        finally
        {
            client.Close();

            pbs.RemoveAll((PlayerBlock pb) => { return pb.name == name; });

            Console.WriteLine($"Client disconnected with {info}");
        }
    }

    static public void Renew()
    {
        foreach (PlayerBlock pb in pbs)
        {
            pb.Revive();
        }
        pfs.Clear();
        pfs.Add(new PlatformBlock(200, 700, 200, 10, PlatformType.Norm));
        ms = 0;
        gameStart = false;
    }
    static public string GetEnvironmentString()
    {
        string result = String.Empty;
        result += String.Join('|', pbs) + '\n';
        result += String.Join('|', pfs) + '\n';
        result += Convert.ToString(ms) + '\n';

        if (!gameStart)
        {
            result += "Game Start in " + Convert.ToString((10000 - ms) / 1000) + '\n';
        }
        if (winner != String.Empty)
        {
            result += "Winner: " + winner;
        }

        return result;
    }

    static public void NextTick()
    {
        if (pbs.Count == 0) return;

        ms += tick;
        if (!gameStart && ms > 10000)
        {
            gameStart = true;
            ms = 0;
            winner = String.Empty;
        }

        foreach (PlatformBlock pf in pfs)
        {
            pf.NextPosition();
        }
        pfs.RemoveAll((PlatformBlock pf) => { return pf.y < 0; });

        foreach (PlayerBlock pb in pbs)
        {
            pb.NextPosition();
            foreach (PlatformBlock pf in pfs)
            {
                pb.CalulateRelation(pf);
            }
        }
        int playerCount = pbs.Count(pb => { return pb.heart > 0; });
        if (gameStart && playerCount == 1)
        {
            winner = pbs.Find(pb => { return pb.heart > 0; }).name;
        }


        if (gameStart && pfs.Count < 5 && ms % 1000 == 0)
        {
            pfs.Add(GeneratePlatform());
        }
    }
    public static PlatformBlock GeneratePlatform()
    {
        var rand = new Random();
        int x = rand.Next(100, 600);
        int y = 1000;
        int w = rand.Next(100, 200);
        int h = 10;

        Array types = Enum.GetValues(typeof(PlatformType));
        PlatformType randomType = (PlatformType)types.GetValue(rand.Next(types.Length));

        return new PlatformBlock(x, y, w, h, randomType);
    }
    static public long GetCurrentTimeMS()
    {
        return DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
    }
}

abstract public class Block
{
    public double x;
    public double y;
    public double w;
    public double h;

    public Block(double _x, double _y, double _w, double _h)
    {
        x = _x;
        y = _y;
        w = _w;
        h = _h;
    }
    abstract override public string ToString();
    abstract public void NextPosition();
    public bool DetectCollision(Block b)
    {
        if (x + w > b.x && x < b.x + b.w && y + h > b.y && y < b.y + b.h)
        {
            return true;
        }
        return false;
    }
    public bool IsOn(Block b)
    {
        if (!(x + w < b.x || x > b.x + b.w) && y + h == b.y)
        {
            return true;
        }
        return false;
    }
}

public enum Direction
{
    None,
    Left,
    Right,
}

 public class PlayerBlock : Block
{
    public int heart;
    public string name;
    public Direction dir;
    public PlayerBlock(double _x, double _y, double _w, int _h, string _name) : base(_x, _y, _w, _h)
    {
        heart = 100;
        name = _name;
        dir = Direction.None;
    }

    public void ChangeDirection(string msg)
    {
        if (msg == "1") dir = Direction.None;
        if (msg == "2") dir = Direction.Left;
        if (msg == "3") dir = Direction.Right;
    }

    override public string ToString()
    {
        return String.Join(',', Math.Floor(x), Math.Floor(y), Math.Floor(w), Math.Floor(h), heart, name);
    }

    public void CalulateRelation(PlatformBlock pb)
    {
        if (DetectCollision(pb))
        {
            if (x < pb.x && x + w > pb.x) x = pb.x - w;
            if (x > pb.x && x < pb.x + pb.w) x = pb.x + w;
            if (y < pb.y && y + h > pb.y) y = pb.y - h;
            if (y > pb.y && y < pb.y + pb.h) y = pb.y - h;
        }
        if (pb.type == PlatformType.Spike && IsOn(pb))
        {
            heart -= 1;
        }
    }
    override public void NextPosition()
    {
        if (dir == Direction.Left) x -= 0.1;
        if (dir == Direction.Right) x += 0.1;
        y += 0.1;
    }

    public void Revive()
    {
        heart = 100;
    }
}

public enum PlatformType
{
    Norm,
    Spike,
}

public class PlatformBlock : Block
{
    public PlatformType type = PlatformType.Norm;
    public PlatformBlock(double _x, double _y, double _w, double _h, PlatformType _type) : base(_x, _y, _w, _h)
    {
        type = _type;
    }
    override public string ToString()
    {
        return String.Join(',', Math.Floor(x), Math.Floor(y), Math.Floor(w), Math.Floor(h), (int)type);
    }

    override public void NextPosition()
    {
        y -= 0.1;
    }
}