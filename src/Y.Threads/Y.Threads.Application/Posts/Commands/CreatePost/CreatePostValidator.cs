using FluentValidation;
using Y.Threads.Domain.Aggregates.Post;

namespace Y.Threads.Application.Posts.Commands.CreatePost;
public sealed class CreatePostValidator : AbstractValidator<CreatePostCommand>
{
    public CreatePostValidator()
    {
        RuleFor(x => x.Author.Id).NotEmpty();
        RuleFor(x => x.Author.Birthdate).NotNull();
        RuleFor(x => x.Author.Name)
            .NotNull()
            .NotEmpty();

        RuleFor(x => x.Text).MaximumLength(280);
        RuleFor(x => x)
            .Must(post => !string.IsNullOrWhiteSpace(post.Text) || !string.IsNullOrEmpty(post.Text) || post.Medias.Count > 0)
            .WithMessage("Post must contain text or at least one media.");

        RuleFor(x => x.Medias.Count()).LessThanOrEqualTo(4);
        RuleForEach(x => x.Medias).ChildRules(fileMedia =>
        {
            fileMedia
                .RuleFor(x => x.Media.ContentType)
                .Must(contentType => Media.IsSupportedMimeType(contentType))
                .WithMessage("Unsupported media type.");
        });
    }
}
