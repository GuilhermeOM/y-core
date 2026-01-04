using Y.Core.SharedKernel;
using Y.Threads.Domain.Errors;
using Y.Threads.Domain.ValueObjects;

namespace Y.Threads.Domain.Aggregates.Post;
public class Post : AggregateRoot
{
    public const int MaxAllowedMedias = 4;
    
    private ICollection<Media> _medias = [];

    public Guid AuthorId { get; private set; }
    public Guid Parent { get; private set; } = Guid.Empty;
    public PostStatus Status { get; private set; }
    public string Text { get; private set; } = string.Empty;
    public long LikeAmount { get; private set; }
    public long ReplyAmount { get; private set; }
    public IReadOnlyCollection<Media> Medias
    {
        get => [.. _medias];
        private set => _medias = [.. value];
    }

    private Post()
    {
    }

    private Post(Guid authorId, string text, PostStatus status)
    {
        AuthorId = authorId;
        Text = text;
        Status = status;
    }

    public static Result<Post> Create(Guid authorId, string text = "", IReadOnlyCollection<MediaUpload>? medias = null)
    {
        medias ??= [];

        if (authorId == Guid.Empty)
        {
            return Result.Failure<Post>(PostErrors.EmptyAuthor);
        }

        var isPostEmpty = (string.IsNullOrEmpty(text) || string.IsNullOrWhiteSpace(text)) && medias.Count == 0;
        if (isPostEmpty)
        {
            return Result.Failure<Post>(PostErrors.EmptyPost);
        }

        var post = new Post(authorId, text, PostStatus.Published);

        foreach (var media in  medias)
        {
            var postMediaResult = post.AddMedia(media);
            if (postMediaResult.IsFailure)
            {
                return Result.Failure<Post>(postMediaResult.Error);
            }
        }

        return Result.Success(post);
    }

    public Result AddMedia(MediaUpload media)
    {
        if (_medias.Count > MaxAllowedMedias)
        {
            return Result.Failure(PostErrors.MaxMediaExceeded);
        }

        var postMediaResult = Media.Create(media);
        if (postMediaResult.IsFailure)
        {
            return Result.Failure(postMediaResult.Error);
        }

        _medias.Add(postMediaResult.Value);
        return Result.Success();
    }
}
