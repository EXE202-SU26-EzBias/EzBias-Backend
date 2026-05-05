namespace EzBias.Domain.Entities;

public class PhotocardDetail
{
    public long ProductId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public string AlbumSeries { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public bool IsPob { get; set; } = false;

    public Product Product { get; set; } = null!;
}
