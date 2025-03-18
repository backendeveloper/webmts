namespace CustomerService.Common.Logging;

public class TraceContext
{
    private readonly AsyncLocal<string> _traceId = new AsyncLocal<string>();

    public string TraceId
    {
        get => _traceId.Value ??= Guid.NewGuid().ToString();
        set => _traceId.Value = value;
    }
}