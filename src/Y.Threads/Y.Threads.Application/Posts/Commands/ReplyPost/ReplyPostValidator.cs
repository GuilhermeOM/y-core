using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using Y.Threads.Domain.Aggregates.Post;

namespace Y.Threads.Application.Posts.Commands.ReplyPost;

public sealed class ReplyPostValidator : AbstractValidator<ReplyPostCommand>
{
    public ReplyPostValidator()
    {
        RuleFor(x => x.Parent).NotEmpty();

        RuleFor(x => x.Author.Id).NotEmpty();
        RuleFor(x => x.Author.Birthdate).NotNull();
        RuleFor(x => x.Author.Name)
            .NotNull()
            .NotEmpty();

        RuleFor(x => x.Text).MaximumLength(280);
        RuleFor(x => x)
            .Must(reply => !string.IsNullOrWhiteSpace(reply.Text) || !string.IsNullOrEmpty(reply.Text) || reply.Medias.Count > 0)
            .WithMessage("Reply must contain text or at least one media.");

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
