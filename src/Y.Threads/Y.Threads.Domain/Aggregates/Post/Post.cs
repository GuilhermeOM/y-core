using Y.Core.SharedKernel;
using Y.Core.SharedKernel.Models;
using Y.Threads.Domain.Errors;
using Y.Threads.Domain.Events;
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
    public IReadOnlyCollection<Media> Medias
    {
        get => [.. _medias];
        private set => _medias = [.. value];
    }

    private Post(Guid authorId, string text, PostStatus status)
    {
        AuthorId = authorId;
        Text = text;
        Status = status;
    }

    public static Result<Post> Create(Author author, string text = "", IReadOnlyCollection<FileUploadResult>? medias = null)
    {
        medias ??= [];

        if (author is null || author.Id == Guid.Empty)
        {
            return Result.Failure<Post>(PostErrors.EmptyAuthor);
        }

        var isPostEmpty = string.IsNullOrWhiteSpace(text) && medias.Count == 0;
        if (isPostEmpty)
        {
            return Result.Failure<Post>(PostErrors.EmptyPost);
        }

        var post = new Post(author.Id, text, PostStatus.Published);

        foreach (var media in medias)
        {
            var postMediaResult = post.AddMedia(media);
            if (postMediaResult.IsFailure)
            {
                return Result.Failure<Post>(postMediaResult.Error);
            }
        }

        post.RaiseDomainEvent(new PostCreatedEvent(post.Id, author, post.Text, post.Medias));

        return Result.Success(post);
    }

    public Result<Post> Reply(Author author, string text = "", IReadOnlyCollection<FileUploadResult>? medias = null)
    {
        medias ??= [];

        if (author is null || author.Id == Guid.Empty)
        {
            return Result.Failure<Post>(PostErrors.EmptyAuthor);
        }

        var isReplyEmpty = string.IsNullOrWhiteSpace(text) && medias.Count == 0;
        if (isReplyEmpty)
        {
            return Result.Failure<Post>(PostErrors.EmptyPost);
        }

        var reply = new Post(author.Id, text, PostStatus.Published)
        {
            Parent = Id
        };

        foreach (var media in medias)
        {
            var replyMediaResult = reply.AddMedia(media);
            if (replyMediaResult.IsFailure)
            {
                return Result.Failure<Post>(replyMediaResult.Error);
            }
        }

        reply.RaiseDomainEvent(new PostRepliedEvent(reply.Id, reply.Parent, author, reply.Text, reply.Medias));

        return Result.Success(reply);
    }

    public Result AddMedia(FileUploadResult media)
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

    public Result Like(Guid userId)
    {
        if (Status != PostStatus.Published)
        {
            return Result.Failure(PostErrors.LikeUnpublishedPost);
        }

        RaiseDomainEvent(new PostLikedEvent(Id, userId));
        return Result.Success();
    }

    public Result Dislike(Guid userId)
    {
        RaiseDomainEvent(new PostDislikedEvent(Id, userId));
        return Result.Success();
    }

    public Result Hide()
    {
        if (Status != PostStatus.Published)
        {
            return Result.Failure(PostErrors.LikeUnpublishedPost);
        }

        Status = PostStatus.Hidden;
        return Result.Success();
    }
}
