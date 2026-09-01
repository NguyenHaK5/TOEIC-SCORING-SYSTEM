using FluentValidation;
using Toeic.Application.RubricManagement.Dtos;

namespace Toeic.Application.RubricManagement.Validators;

public class CreateRubricSetRequestValidator : AbstractValidator<CreateRubricSetRequest>
{
    public CreateRubricSetRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Version).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.QuestionType).IsInEnum();
    }
}

public class UpdateRubricSetRequestValidator : AbstractValidator<UpdateRubricSetRequest>
{
    public UpdateRubricSetRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
    }
}

public class CreateRubricRequestValidator : AbstractValidator<CreateRubricRequest>
{
    public CreateRubricRequestValidator()
    {
        RuleFor(x => x.Criterion).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Level).GreaterThan(0);
        RuleFor(x => x.MinScore).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MaxScore).GreaterThanOrEqualTo(x => x.MinScore)
            .WithMessage("MaxScore must be greater than or equal to MinScore.");
        RuleFor(x => x.Weight).InclusiveBetween(0m, 1m)
            .WithMessage("Weight must be between 0 and 1 (e.g. 0.25 = 25%).");
    }
}

public class UpdateRubricRequestValidator : AbstractValidator<UpdateRubricRequest>
{
    public UpdateRubricRequestValidator()
    {
        RuleFor(x => x.Criterion).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Level).GreaterThan(0);
        RuleFor(x => x.MinScore).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MaxScore).GreaterThanOrEqualTo(x => x.MinScore)
            .WithMessage("MaxScore must be greater than or equal to MinScore.");
        RuleFor(x => x.Weight).InclusiveBetween(0m, 1m)
            .WithMessage("Weight must be between 0 and 1 (e.g. 0.25 = 25%).");
    }
}
