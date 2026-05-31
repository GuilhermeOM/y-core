using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;

namespace Y.Threads.Application.Threads.Queries.GetThreadById;

public sealed class GetThreadByIdValidator : AbstractValidator<GetThreadByIdQuery>
{
    public GetThreadByIdValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.MaxDepth).GreaterThanOrEqualTo(0);
    }
}
