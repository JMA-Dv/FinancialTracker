using FinancialTracker.Api.Features.Auth;
using FinancialTracker.Application.Categories;
using FinancialTracker.Domain.Categories;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace FinancialTracker.Api.Features.Categories;

[ApiController]
[Route("api/categories")]
public class CategoriesController(
    ListCategories listCategories,
    CreateCategory createCategory,
    RenameCategory renameCategory,
    MoveCategory moveCategory,
    ArchiveCategory archiveCategory,
    IValidator<CreateCategoryRequest> createValidator,
    IValidator<RenameCategoryRequest> renameValidator)
    : ControllerBase
{
    /// <summary>The Categories as a tree. Archived ones, and what is under them, are left out unless asked for.</summary>
    [HttpGet]
    public async Task<IReadOnlyList<CategoryTreeResponse>> List([FromQuery] bool includeArchived = false) =>
        (await listCategories.ExecuteAsync(User.Id(), includeArchived)).Select(CategoryTreeResponse.Of).ToList();

    [HttpPost]
    public async Task<ActionResult<CategoryResponse>> Create(CreateCategoryRequest request)
    {
        if (await InvalidAsync(createValidator, request) is { } invalid)
        {
            return invalid;
        }

        var category = await createCategory.ExecuteAsync(User.Id(), request.Name, request.Kind, request.ParentId);
        return StatusCode(StatusCodes.Status201Created, CategoryResponse.Of(category));
    }

    [HttpPut("{id:guid}/name")]
    public async Task<ActionResult<CategoryResponse>> Rename(Guid id, RenameCategoryRequest request)
    {
        if (await InvalidAsync(renameValidator, request) is { } invalid)
        {
            return invalid;
        }

        return CategoryResponse.Of(await renameCategory.ExecuteAsync(User.Id(), id, request.Name));
    }

    /// <summary>Moves the Category under another parent, or to the top level when ParentId is left out.</summary>
    [HttpPut("{id:guid}/parent")]
    public async Task<CategoryResponse> Move(Guid id, MoveCategoryRequest request) =>
        CategoryResponse.Of(await moveCategory.ExecuteAsync(User.Id(), id, request.ParentId));

    [HttpPost("{id:guid}/archive")]
    public async Task<CategoryResponse> Archive(Guid id) =>
        CategoryResponse.Of(await archiveCategory.ExecuteAsync(User.Id(), id, archived: true));

    [HttpPost("{id:guid}/unarchive")]
    public async Task<CategoryResponse> Unarchive(Guid id) =>
        CategoryResponse.Of(await archiveCategory.ExecuteAsync(User.Id(), id, archived: false));

    /// <summary>A 400 listing every broken rule, or null when the request is valid and can go on to the use case.</summary>
    private async Task<ActionResult?> InvalidAsync<T>(IValidator<T> validator, T request)
    {
        var result = await validator.ValidateAsync(request, HttpContext.RequestAborted);
        if (result.IsValid)
        {
            return null;
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
        }

        return ValidationProblem(ModelState);
    }
}

internal sealed class CreateCategoryRequestValidator : AbstractValidator<CreateCategoryRequest>
{
    public CreateCategoryRequestValidator()
    {
        RuleFor(r => r.Name).NotNull().MaximumLength(Category.MaxNameLength);
        RuleFor(r => r.Kind).IsInEnum().When(r => r.Kind is not null);
    }
}

internal sealed class RenameCategoryRequestValidator : AbstractValidator<RenameCategoryRequest>
{
    public RenameCategoryRequestValidator() => RuleFor(r => r.Name).NotNull().MaximumLength(Category.MaxNameLength);
}

/// <param name="Kind">Required for a top-level Category. A child takes its parent's kind, and must match it if given.</param>
/// <param name="ParentId">A top-level Category to sit under. Leave it out for a top-level Category.</param>
public sealed record CreateCategoryRequest(string Name, CategoryKind? Kind, Guid? ParentId);

public sealed record RenameCategoryRequest(string Name);

public sealed record MoveCategoryRequest(Guid? ParentId);

public sealed record CategoryResponse(Guid Id, string Name, CategoryKind Kind, Guid? ParentId, bool IsArchived)
{
    public static CategoryResponse Of(Category category) =>
        new(category.Id, category.Name, category.Kind, category.ParentId, category.IsArchived);
}

public sealed record CategoryTreeResponse(
    Guid Id, string Name, CategoryKind Kind, bool IsArchived, IReadOnlyList<CategoryTreeResponse> Children)
{
    public static CategoryTreeResponse Of(CategoryNode node) =>
        new(node.Category.Id, node.Category.Name, node.Category.Kind, node.Category.IsArchived, node.Children.Select(Of).ToList());
}
