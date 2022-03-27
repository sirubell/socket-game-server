class Server
{
    static public List<PlayerBlock> pbs = new List<PlayerBlock>();
    static public List<PlatformBlock> pfs = new List<PlatformBlock>();
    static public int tick;
    public static void Main()
    {
        Renew();
        pbs.Add(new PlayerBlock(0, 0, 10, 10));
        pbs.Add(new PlayerBlock(10, 10, 10, 10));
        pfs.Add(new PlatformBlock(0, 100, 100, 100, PlatformType.Norm));

        Console.WriteLine(GetEnvironmentString());
        long prevTime = GetCurrentTimeMS();

        while (true)
        {
            long currentTime = GetCurrentTimeMS();
            if (currentTime >= prevTime + 5)
            {
                prevTime = currentTime;

                NextTick();

                Console.WriteLine(GetEnvironmentString());
            }
            
        }
    }
    static public void Renew()
    {
        pbs.Clear();
        pfs.Clear();
        tick = 0;
    }
    static public string GetEnvironmentString()
    {
        string result = String.Empty;
        result += String.Join(',', pbs) + '\n';
        result += String.Join(',', pfs) + '\n';

        return result;
    }

    static public void NextTick()
    {
        tick++;

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
                pb.adjustPosition(pf);
            }
        }

        // tick == 5ms, so tick * 200 == 1sec
        if (pfs.Count < 5 && tick % 200 == 0)
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
}

public enum CurrentDirection
{
    None,
    Left,
    Right,
}

 public class PlayerBlock : Block
{
    int heart;
    CurrentDirection dir;
    public PlayerBlock(double _x, double _y, double _w, int _h) : base(_x, _y, _w, _h)
    {
        heart = 100;
        dir = CurrentDirection.None;
    }
    
    override public string ToString()
    {
        return '[' + String.Join(',', Math.Floor(x), Math.Floor(y), Math.Floor(w), Math.Floor(h), heart) + ']';
    }

    public void adjustPosition(PlatformBlock pb)
    {
        if (DetectCollision(pb))
        {
            if (x < pb.x && x + w > pb.x) x = pb.x - w;
            if (x > pb.x && x < pb.x + pb.w) x = pb.x + w;
            if (y < pb.y && y + h > pb.y) y = pb.y - h;
            if (y > pb.y && y < pb.y + pb.h) y = pb.y - h;
        }
    }
    override public void NextPosition()
    {
        if (dir == CurrentDirection.Left) x -= 0.1;
        if (dir == CurrentDirection.Right) x += 0.1;
        y += 0.1;
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
    public PlatformBlock(double _x, double _y, double _w, double _h, PlatformType _type) : base(_x, _y, _w, _h)
    {
        type = _type;
    }
    override public string ToString()
    {
        return '[' + String.Join(',', Math.Floor(x), Math.Floor(y), Math.Floor(w), Math.Floor(h), (int)type) + ']';
    }

    override public void NextPosition()
    {
        y -= 0.1;
    }
}