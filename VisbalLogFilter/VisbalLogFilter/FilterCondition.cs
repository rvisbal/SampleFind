public class FilterCondition
{
    public string Type { get; set; }
    public string Value { get; set; }

    public bool Matches(string line)
    {
        return Type switch
        {
            "CONTAINS" => line.Contains(Value, StringComparison.OrdinalIgnoreCase),
            "EQUALS" => line.Equals(Value, StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }
} 