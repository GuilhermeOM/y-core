using FluentValidation;

namespace Y.Threads.Application.Posts.UseCases.CreatePost;
internal sealed class CreatePostValidator : AbstractValidator<CreatePostUseCase>
{
    public CreatePostValidator()
    {
        RuleFor(x => x.Author.Id).NotEmpty();
        RuleFor(x => x.Author.Birthdate).NotNull();
        RuleFor(x => x.Author.Name)
            .NotNull()
            .NotEmpty();

        RuleFor(x => x.Text).MaximumLength(280);

        RuleFor(x => x.Medias.Count()).LessThanOrEqualTo(4);
    }
}
