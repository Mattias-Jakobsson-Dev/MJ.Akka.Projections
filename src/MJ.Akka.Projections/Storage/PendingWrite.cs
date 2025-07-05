using System.Collections.Immutable;

namespace MJ.Akka.Projections.Storage;

public class PendingWrite
{
    private readonly IImmutableList<TaskCompletionSource<StoreProjectionResponse>> _completions;

    public static PendingWrite Empty { get; } = new(
        StoreProjectionRequest.Empty,
        ImmutableList<TaskCompletionSource<StoreProjectionResponse>>.Empty,
        CancellationToken.None);

    public PendingWrite(
        StoreProjectionRequest request,
        TaskCompletionSource<StoreProjectionResponse> completion,
        CancellationToken cancellationToken) : this(
        request,
        ImmutableList.Create(completion),
        cancellationToken)
    {
    }

    private PendingWrite(
        StoreProjectionRequest request,
        IImmutableList<TaskCompletionSource<StoreProjectionResponse>> completions,
        CancellationToken cancellationToken)
    {
        Request = request;
        _completions = completions;
        CancellationToken = cancellationToken;
    }
    
    public StoreProjectionRequest Request { get; }
    public CancellationToken CancellationToken { get; }
    
    public bool IsEmpty => Request.IsEmpty && _completions.Count == 0;

    public PendingWrite MergeWith(PendingWrite other)
    {
        var cancellationToken = CancellationToken == other.CancellationToken
            ? CancellationToken
            : CancellationTokenSource
                .CreateLinkedTokenSource(CancellationToken, other.CancellationToken)
                .Token;
        
        return new PendingWrite(
            Request.MergeWith(other.Request),
            _completions.AddRange(other.GetCompletions()),
            cancellationToken);
    }

    public void Completed(StoreProjectionResponse response)
    {
        foreach (var completion in _completions)
            completion.TrySetResult(response);
    }

    public void Fail(Exception exception)
    {
        foreach (var completion in _completions)
            completion.TrySetException(exception);
    }

    private IImmutableList<TaskCompletionSource<StoreProjectionResponse>> GetCompletions()
    {
        return _completions;
    }
}