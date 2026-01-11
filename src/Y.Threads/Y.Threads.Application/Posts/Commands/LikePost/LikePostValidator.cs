using FluentValidation;

namespace Y.Threads.Application.Posts.Commands.LikePost;
public sealed class LikePostValidator : AbstractValidator<LikePostCommand>
{
    public LikePostValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.PostId).NotEmpty();
    }
}
