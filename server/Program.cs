using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

/// <summary>
/// 
/// </summary>

class Server
{
    public static void Main()
    {
        GameState.Renew();
        GameState.pbs.Add(new PlayerBlock(0, 0, 10, 10));
        GameState.pbs.Add(new PlayerBlock(10, 10, 10, 10));
        GameState.pfs.Add(new PlatformBlock(0, 100, 100, 100, PlatformType.Norm));

        Console.WriteLine(GameState.GetEnvironmentString());

        while (!GameState.isEnd())
        {
            if (GameState.GetCurrentTimeMS() > GameState.prev_update_time + 5)
            {
                GameState.nextTick();
                

                Console.WriteLine(GameState.GetEnvironmentString());
            }
            
        }
    }
}

abstract public class Block
{
    public int x;
    public int y;
    public int w;
    public int h;

    public Block(int _x, int _y, int _w, int _h)
    {
        x = _x;
        y = _y;
        w = _w;
        h = _h;
    }
    abstract override public string ToString();
}//123

 public class PlayerBlock : Block
{
    int heart;
    public PlayerBlock(int _x, int _y, int _w, int _h) : base(_x, _y, _w, _h)
    {
        heart = 100;
    }
    
    override public string ToString()
    {
        return '[' + String.Join(',', x, y, w, h, heart) + ']';
    }

    public void down()
    {
        y += 1;
    }

    public void adjustPosition(PlatformBlock pb)
    {
        if (y + h > pb.y)
        {
            y = pb.y - h;
        }
        if (x + w > pb.x)
        {
            x = pb.x - w;
        }
        if (x < pb.x + w)
        {
            x = pb.x + w;
        }
    }
}

public enum PlatformType
{
    Norm,
    Spike,
}

public class PlatformBlock : Block
{
    PlatformType type = PlatformType.Norm;
    public PlatformBlock(int _x, int _y, int _w, int _h, PlatformType _type) : base(_x, _y, _w, _h)
    {
        type = _type;
    }
    override public string ToString()
    {
        return '[' + String.Join(',', x, y, w, h, type) + ']';
    }

    public void up()
    {
        y -= 1;
    }
}

static public class GameState
{
    static public List<PlayerBlock> pbs = new List<PlayerBlock>();
    static public List<PlatformBlock> pfs = new List<PlatformBlock>();
    static public long prev_update_time;
    static public long prev_update_platform_time;

    static public void Renew()
    {
        pbs.Clear();
        pfs.Clear();
        prev_update_time = GetCurrentTimeMS();
        prev_update_platform_time = GetCurrentTimeMS();
    }

    static public string GetEnvironmentString()
    {
        string result = String.Empty;
        result += String.Join(',', pbs) + '\n';
        result += String.Join(',', pfs) + '\n';

        return result;
    }

    static public long GetCurrentTimeMS()
    {
        return DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
    }

    static public bool isEnd()
    {
        return false;
    }

    static public void nextTick()
    {
        long current_time = GetCurrentTimeMS();
        prev_update_time = current_time;

        foreach (PlatformBlock pf in GameState.pfs)
        {
            pf.up();
        }
        GameState.pfs.RemoveAll((PlatformBlock pf) => { return pf.y < 0; });
        // remove pf if y < 0

        foreach (PlayerBlock pb in GameState.pbs)
        {
            pb.down();
            foreach (PlatformBlock pf in GameState.pfs)
            {
                pb.adjustPosition(pf);
            }
        }

        if (current_time > prev_update_platform_time + 1000 && pfs.Count < 5)
        {
            prev_update_platform_time = current_time;
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
}