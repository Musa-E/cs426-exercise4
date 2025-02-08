// namespace PartObject
public class Part
{
    public string Name;
    public int Count;
    public bool WasTurnedIn;

    // Constructor
    public Part(string name, int count, bool wasTurnedIn)
    {
        Name = name;
        Count = count;
        WasTurnedIn = wasTurnedIn;
    }
}
