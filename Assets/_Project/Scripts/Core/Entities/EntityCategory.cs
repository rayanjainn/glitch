namespace BugReport.Core.Entities
{
    /// <summary>
    /// Broad gameplay classification of an entity. Glitch definitions allow or
    /// forbid categories; the anomaly itself is never a valid glitch target.
    /// </summary>
    public enum EntityCategory
    {
        Prop = 0,
        Structure = 1,
        Decoration = 2,
        Hero = 3,
        Enemy = 4,
        Objective = 5,
        SystemDevice = 6,
        Anomaly = 7,
    }
}
