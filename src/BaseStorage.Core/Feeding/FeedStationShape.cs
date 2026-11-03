namespace BaseStorage.Core.Feeding;

/// <summary>The kind of station a feed state describes.</summary>
public enum FeedStationShape
{
    /// <summary>A station with a queue of inputs and, optionally, fuel, such as a smelter, kiln or windmill.</summary>
    SmelterClass = 0,

    /// <summary>A station that only takes fuel, such as a campfire or a standing torch.</summary>
    FuelOnly = 1,

    /// <summary>A station that holds one input at a time, such as a fermenter.</summary>
    SingleInput = 2,

    /// <summary>A station with several slots that each hold one input, such as a cooking station.</summary>
    SeveralInput = 3,
}
