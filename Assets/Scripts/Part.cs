// namespace PartObject
public class Part
{
    public string Name;       // The part's name
    public int Count;         // The number of items; this will likely not be used/will be deprecated
    public bool WasTurnedIn;  // Has this item been turned in before?

    // Is this part a member of the Input Devices, CPU, or Output Devices?
    private string memberOf;

    /// <summary>
    /// Constructor: part name, number of items, has it been turned in before?
    /// </summary>
    /// <param name="name"> The name of the part </param>
    /// <param name="count"> The number of parts (this may be deprecated) </param>
    /// <param name="wasTurnedIn"> Has this item been turned in before? </param>
    public Part(string name, int count, bool wasTurnedIn)
    {
        Name = name;
        Count = count;
        WasTurnedIn = wasTurnedIn;
    }

    /// <summary>
    /// Constructor that also accepts category directly
    /// </summary>
    /// <param name="name"></param>
    /// <param name="count"></param>
    /// <param name="wasTurnedIn"></param>
    /// <param name="category"></param>
    public Part(string name, int count, bool wasTurnedIn, string category)
    {
        Name = name;
        Count = count;
        WasTurnedIn = wasTurnedIn;
        memberOf = category;
    }


    /// <summary>
    /// Returns which category this item belongs to
    /// </summary>
    /// <returns></returns>
    public string GetMemberOf() {
        return memberOf;
    }

    /// <summary>
    /// Sets the category this part belongs to; Input, CPU, or Output devices
    /// </summary>
    /// <param name="memberCategory"> Name of the category </param>
    public void SetMemberOf(string memberCategory) {
        memberOf = memberCategory;
    }

}
