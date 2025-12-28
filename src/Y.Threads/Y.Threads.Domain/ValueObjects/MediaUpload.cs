namespace Y.Threads.Domain.ValueObjects;

public sealed class MediaUpload
{
    public string Name { get; set; }
    public string Url { get; set; }
    public string Mime { get; set; }
    public string Description { get; set; } = string.Empty;

    public MediaUpload(string name, string url, string mime)
    {
        Name = name;
        Url = url;
        Mime = mime;
    }
}
