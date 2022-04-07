using System.Net;
using System.Net.Sockets;
using System.Text;
class Server
{
    static public List<PlayerBlock> pbs = new List<PlayerBlock>();
    static public List<PlatformBlock> pfs = new List<PlatformBlock>();
    static long ms;
    static int platformBlockCounter;
    public const long tick = 5;
    static bool gameStart;
    static string winner = String.Empty;
    static string currentEnvironment = String.Empty;
    public static void Main()
    {
        StartServer();
        Renew();

        long prevTime = GetCurrentTimeMS();
        while (true)
        {
            long currentTime = GetCurrentTimeMS();
            if (currentTime >= prevTime + tick)
            {
                prevTime = currentTime;
                currentEnvironment = GetEnvironmentString();

                NextTick();
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
        Int32 port = 15070;
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

                StartClientThread(client, Convert.ToString(counter));
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

        pbs.Add(new PlayerBlock(300, 100, 50, 50, name, gameStart == true ? 0 : 100));

        try
        {
            NetworkStream stream = client.GetStream();
            string receivedData = String.Empty;

            while (ReceiveData(stream, out receivedData))
            {
                //Console.WriteLine($"Receive {receivedData} from client: {info}");

                PlayerBlock player = pbs.Find((PlayerBlock pb) => { return pb.name == name; });
                player.ChangeDirection(receivedData);

                string msg = name + "\n" + currentEnvironment;

                SendData(stream, msg);
                //Console.WriteLine($"Send {msg} to client: {info}");

                //Console.WriteLine("if this message spam too much, it means the code has a bug somewhere.");
            }

        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
        finally
        {
            client.Close();
            Console.WriteLine($"Client disconnected with {info}");

            pbs.RemoveAll((PlayerBlock pb) => { return pb.name == name; });
        }
    }
    static bool ReceiveData(NetworkStream stream, out string data)
    {
        byte[] buffer = new byte[8];
        StringBuilder msg = new StringBuilder();
        data = String.Empty;

        do
        {
            Int32 bytes = stream.Read(buffer, 0, buffer.Length);
            msg.Append(Encoding.ASCII.GetString(buffer, 0, bytes));
        } while (stream.DataAvailable);

        data = msg.ToString();
        return data != String.Empty;
    }

    static void SendData(NetworkStream stream, string msg)
    {
        byte[] buffer = Encoding.ASCII.GetBytes(msg);
        stream.Write(buffer, 0, buffer.Length);
    }

    static public void Renew()
    {
        foreach (PlayerBlock pb in pbs)
        {
            pb.Revive();
        }
        pfs.Clear();
        pfs.Add(new PlatformBlock(200, 700, 200, 10, "1", PlatformType.Norm));
        ms = 0;
        platformBlockCounter = 1;
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
            result += "Game Start in " + Convert.ToString((20000 - ms) / 1000) + '\n';
        }
        if (winner != String.Empty)
        {
            result += "Winner: " + winner;
        }

        return result;
    }

    static public void NextTick()
    {
        ms += tick;
        if (!gameStart && ms > 20000)
        {
            gameStart = true;
            ms = 0;
            winner = String.Empty;
        }

        PlayerDown();
        if (gameStart) PlatformUp();
        pfs.RemoveAll((PlatformBlock pf) => { return pf.y < 0; });
        AdjustPlayerPosition();

        PlayerGoDirection();
        AdjustPlayerPosition();

        CalculateDamage();

        foreach (PlayerBlock pb in pbs)
        {
            if (pb.y <= 0 || pb.y + pb.h >= 900) pb.heart = 0;
        }

        int playerAliveCount = pbs.Count(pb => { return pb.heart > 0; });
        if (gameStart && playerAliveCount == 1)
        {
            winner = pbs.Find(pb => { return pb.heart > 0; }).name;
        }
        if (pbs.Count == 0 || playerAliveCount == 0)
        {
            Renew();
        }

        if (gameStart && ms % 600 == 0)
        {
            pfs.Add(GeneratePlatform());
        }
    }
    public static PlatformBlock GeneratePlatform()
    {
        var rand = new Random();
        int w = rand.Next(100, 250);
        int h = 10;
        int x = rand.Next(100, 501);
        int y = 900;
        

        Array types = Enum.GetValues(typeof(PlatformType));
        PlatformType type;

        if (pfs.Count(pf => pf.type == PlatformType.Norm) == 0)
        {
            type = PlatformType.Norm;
        }
        else
        {
            int randomVal = rand.Next(10);
            type = randomVal < 3 ? PlatformType.Spike : PlatformType.Norm;
        }
        
        return new PlatformBlock(x - w / 2, y, w, h, Convert.ToString(++platformBlockCounter), type);
    }
    static public long GetCurrentTimeMS()
    {
        return DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
    }

    static void PlayerDown()
    {
        foreach (PlayerBlock pb in pbs)
        {
            pb.y += 1 + 0.3 * (ms / 10000);
        }
    }
    static void PlatformUp()
    {
        foreach (PlatformBlock pf in pfs)
        {
            pf.y -= 1 + 0.3 * (ms / 10000);
        }
    }
    static void AdjustPlayerPosition()
    {
        foreach(PlayerBlock pb in pbs)
        {
            foreach (PlatformBlock pf in pfs)
            {
                pb.CalulateRelation(pf);
            }
            if (pb.x + pb.w > 600) pb.x = pb.x + pb.w - 600;
            if (pb.x < 0) pb.x = 600 - pb.w;
        }
    }
    static void CalculateDamage()
    {
        foreach (PlayerBlock pb in pbs)
        {
            bool damaged = false;
            foreach (PlatformBlock pf in pfs)
            {
                if (pf.type == PlatformType.Spike && pb.IsOn(pf))
                {
                    pb.heart -= 0.1;
                    damaged = true;
                }
            }
            if (!damaged && pb.heart < 100 && pb.heart > 0)
            {
                pb.heart += 0.01;
            }
        }
    }

    static void PlayerGoDirection()
    {
        foreach (PlayerBlock pb in pbs)
        {
            if (pb.dir == Direction.Left) pb.x -= 1 + 0.3 * (ms / 10000);
            if (pb.dir == Direction.Right) pb.x += 1 + 0.3 * (ms / 10000);
        }
    }
}

abstract public class Block
{
    public double x;
    public double y;
    public double w;
    public double h;
    public string name;

    public Block(double _x, double _y, double _w, double _h, string _name)
    {
        x = _x;
        y = _y;
        w = _w;
        h = _h;
        name = _name;
    }
    abstract override public string ToString();
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
    public double heart;
    public Direction dir;
    public PlayerBlock(double _x, double _y, double _w, int _h, string _name, int _heart) : base(_x, _y, _w, _h, _name)
    {
        heart = _heart;
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
        if (heart <= 0) return String.Empty;

        return String.Join(',', Math.Floor(x), Math.Floor(y), Math.Floor(w), Math.Floor(h), name, Math.Floor(heart));
    }

    public void CalulateRelation(PlatformBlock pb)
    {
        if (DetectCollision(pb) && y < pb.y && y + h > pb.y && y + h < pb.y + pb.h) y = pb.y - h;
        if (DetectCollision(pb) && y > pb.y && y < pb.y + pb.h) y = pb.y + pb.h;
        if (DetectCollision(pb) && x < pb.x && x + w > pb.x) x = pb.x - w;
        if (DetectCollision(pb) && x > pb.x && x < pb.x + pb.w) x = pb.x + pb.w;
    }

    public void Revive()
    {
        heart = 100;
        x = 300;
        y = 100;
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
    public PlatformBlock(double _x, double _y, double _w, double _h, string _name, PlatformType _type) : base(_x, _y, _w, _h, _name)
    {
        type = _type;
    }
    override public string ToString()
    {
        return String.Join(',', Math.Floor(x), Math.Floor(y), Math.Floor(w), Math.Floor(h), name, (int)type + 1);
    }
}