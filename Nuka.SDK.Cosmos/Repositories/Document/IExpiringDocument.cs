namespace Nuka.SDK.Cosmos.Repositories.Document
{
    public interface IExpiringDocument: IDocument
    {
        public int? ttl { get; set; }
        public bool? _deleted { get; set; }
    }
}