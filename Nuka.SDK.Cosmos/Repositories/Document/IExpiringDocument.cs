namespace Nuka.SDK.Cosmos.Repositories.Document
{
    internal interface IExpiringDocument: IDocument
    {
        public int? ttl { get; set; }
        public bool? _deleted { get; set; }
    }
}