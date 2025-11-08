using Y.Threads.Domain.Entities.Base;

namespace Y.Threads.Domain.Entities;
public sealed class Thread : Entity
{
    public long Depth { get; set; }
    public Guid PostId { get; set; }
    public long LikeAmount { get; set; }
    public long ReplyAmount { get; set; }
    public long PostAmount { get; set; }
    public ThreadType Type { get; set; }
}

public enum ThreadType
{
    Post = 0,
    Reply = 1,
}
