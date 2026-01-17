using FluentValidation;

namespace Y.Threads.Application.Posts.Commands.DislikePost;
public sealed class DislikePostValidator : AbstractValidator<DislikePostCommand>
{
    public DislikePostValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.PostId).NotEmpty();
    }
}
