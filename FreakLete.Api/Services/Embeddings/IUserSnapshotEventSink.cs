namespace FreakLete.Api.Services.Embeddings;

public interface IUserSnapshotEventSink
{
    void OnUserUpdated(int userId);
}
