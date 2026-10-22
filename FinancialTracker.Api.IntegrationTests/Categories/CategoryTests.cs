using System.Net;
using System.Net.Http.Json;
using FinancialTracker.Api.IntegrationTests.Infrastructure;

namespace FinancialTracker.Api.IntegrationTests.Categories;

[Collection(ApiCollection.Name)]
public sealed class CategoryTests(ApiFactory api)
{
    private const string CategoriesEndpoint = "/api/categories";

    [Fact]
    public async Task A_new_User_sees_the_starter_Categories_as_a_tree_of_Income_and_Expense()
    {
        var user = await api.CreateSignedInUserAsync();

        var tree = await ListAsync(user);

        var food = Assert.Single(tree, node => node.Name == "Food");
        Assert.Equal("Expense", food.Kind);
        Assert.Equal(["Groceries", "Restaurants"], food.Children.Select(child => child.Name));
        Assert.All(food.Children, child => Assert.Equal("Expense", child.Kind));
        Assert.Equal("Income", Assert.Single(tree, node => node.Name == "Salary").Kind);
    }

    [Fact]
    public async Task Starter_Categories_belong_to_each_User_separately()
    {
        var first = await api.CreateSignedInUserAsync();
        var second = await api.CreateSignedInUserAsync();
        var food = Assert.Single(await ListAsync(first), node => node.Name == "Food");

        await first.Client.PutAsJsonAsync($"{CategoriesEndpoint}/{food.Id}/name", new { name = "Eating" });

        Assert.Contains(await ListAsync(second), node => node.Name == "Food");
    }

    [Fact]
    public async Task A_User_can_create_a_top_level_Category_and_a_child_taking_the_parents_kind()
    {
        var user = await api.CreateSignedInUserAsync();

        var pets = await CreateAsync(user, new { name = "Pets", kind = "Expense" });
        var vet = await CreateAsync(user, new { name = "Vet", parentId = pets.Id });

        Assert.Equal(new CategoryView(vet.Id, "Vet", "Expense", pets.Id, false), vet);
        var node = Assert.Single(await ListAsync(user), n => n.Id == pets.Id);
        Assert.Equal([vet.Id], node.Children.Select(child => child.Id));
    }

    public static TheoryData<string, Func<Guid, object>> RejectedCreations => new()
    {
        { "a top-level Category without a kind", _ => new { name = "Pets" } },
        { "a blank name", _ => new { name = " ", kind = "Expense" } },
        { "a kind that doesn't exist", _ => new { name = "Pets", kind = "Sideways" } },
        { "a child of another kind than its parent", parent => new { name = "Bonus", kind = "Income", parentId = parent } },
        { "a third level", parent => new { name = "Organic", parentId = parent } },
    };

    [Theory]
    [MemberData(nameof(RejectedCreations))]
    public async Task A_Category_that_breaks_a_rule_is_rejected_and_not_created(string rule, Func<Guid, object> category)
    {
        var user = await api.CreateSignedInUserAsync();
        var food = Assert.Single(await ListAsync(user), node => node.Name == "Food");
        var groceries = Assert.Single(food.Children, child => child.Name == "Groceries");
        var before = Count(await ListAsync(user));

        // The third-level case needs a child as the parent; the kind mismatch needs a top-level one.
        var parent = rule.Contains("third") ? groceries.Id : food.Id;
        var created = await user.Client.PostAsJsonAsync(CategoriesEndpoint, category(parent));

        Assert.True(created.StatusCode is HttpStatusCode.BadRequest, $"{rule}: got {(int)created.StatusCode}");
        Assert.Equal(before, Count(await ListAsync(user)));
    }

    [Fact]
    public async Task Names_are_unique_per_parent_ignoring_case()
    {
        var user = await api.CreateSignedInUserAsync();
        var food = Assert.Single(await ListAsync(user), node => node.Name == "Food");
        var housing = Assert.Single(await ListAsync(user), node => node.Name == "Housing");

        var topLevelDuplicate = await user.Client.PostAsJsonAsync(CategoriesEndpoint, new { name = "fOOD", kind = "Expense" });
        var childDuplicate = await user.Client.PostAsJsonAsync(CategoriesEndpoint, new { name = " GROCERIES", parentId = food.Id });
        var sameNameElsewhere = await user.Client.PostAsJsonAsync(CategoriesEndpoint, new { name = "Groceries", parentId = housing.Id });
        var renameToDuplicate = await user.Client.PutAsJsonAsync($"{CategoriesEndpoint}/{housing.Id}/name", new { name = "food" });
        var renameToItself = await user.Client.PutAsJsonAsync($"{CategoriesEndpoint}/{housing.Id}/name", new { name = "HOUSING" });

        Assert.Equal(HttpStatusCode.Conflict, topLevelDuplicate.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, childDuplicate.StatusCode);
        Assert.Equal(HttpStatusCode.Created, sameNameElsewhere.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, renameToDuplicate.StatusCode);
        Assert.Equal(HttpStatusCode.OK, renameToItself.StatusCode);
    }

    [Fact]
    public async Task A_User_can_rename_a_Category_and_move_it_under_another_parent()
    {
        var user = await api.CreateSignedInUserAsync();
        var tree = await ListAsync(user);
        var housing = Assert.Single(tree, node => node.Name == "Housing");
        var groceries = Assert.Single(Assert.Single(tree, node => node.Name == "Food").Children, child => child.Name == "Groceries");

        var renamed = await user.Client.PutAsJsonAsync($"{CategoriesEndpoint}/{groceries.Id}/name", new { name = "Supermarket" });
        var moved = await user.Client.PutAsJsonAsync($"{CategoriesEndpoint}/{groceries.Id}/parent", new { parentId = housing.Id });

        Assert.Equal(HttpStatusCode.OK, renamed.StatusCode);
        Assert.Equal(HttpStatusCode.OK, moved.StatusCode);
        var housingNow = Assert.Single(await ListAsync(user), node => node.Name == "Housing");
        Assert.Contains(housingNow.Children, child => child.Name == "Supermarket");
    }

    [Fact]
    public async Task A_Category_with_children_cant_move_under_another_and_a_child_can_move_to_the_top_level()
    {
        var user = await api.CreateSignedInUserAsync();
        var tree = await ListAsync(user);
        var food = Assert.Single(tree, node => node.Name == "Food");
        var housing = Assert.Single(tree, node => node.Name == "Housing");
        var groceries = Assert.Single(food.Children, child => child.Name == "Groceries");
        var salary = Assert.Single(tree, node => node.Name == "Salary");

        var withChildren = await user.Client.PutAsJsonAsync($"{CategoriesEndpoint}/{food.Id}/parent", new { parentId = housing.Id });
        var underChild = await user.Client.PutAsJsonAsync($"{CategoriesEndpoint}/{housing.Id}/parent", new { parentId = groceries.Id });
        var otherKind = await user.Client.PutAsJsonAsync($"{CategoriesEndpoint}/{groceries.Id}/parent", new { parentId = salary.Id });
        var toTop = await user.Client.PutAsJsonAsync($"{CategoriesEndpoint}/{groceries.Id}/parent", new { });

        Assert.Equal(HttpStatusCode.BadRequest, withChildren.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, underChild.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, otherKind.StatusCode);
        Assert.Equal(HttpStatusCode.OK, toTop.StatusCode);
        Assert.Contains(await ListAsync(user), node => node.Name == "Groceries");
    }

    [Fact]
    public async Task The_tree_hides_Archived_Categories_unless_asked_to_include_them()
    {
        var user = await api.CreateSignedInUserAsync();
        var food = Assert.Single(await ListAsync(user), node => node.Name == "Food");
        var groceries = Assert.Single(food.Children, child => child.Name == "Groceries");

        var archived = await user.Client.PostAsync($"{CategoriesEndpoint}/{groceries.Id}/archive", null);

        Assert.Equal(HttpStatusCode.OK, archived.StatusCode);
        Assert.DoesNotContain(Assert.Single(await ListAsync(user), node => node.Name == "Food").Children, child => child.Id == groceries.Id);
        var all = Assert.Single(await ListAsync(user, includeArchived: true), node => node.Name == "Food");
        Assert.True(Assert.Single(all.Children, child => child.Id == groceries.Id).IsArchived);

        await user.Client.PostAsync($"{CategoriesEndpoint}/{groceries.Id}/unarchive", null);

        Assert.Contains(Assert.Single(await ListAsync(user), node => node.Name == "Food").Children, child => child.Id == groceries.Id);
    }

    [Fact]
    public async Task Another_Users_Category_is_not_found_for_every_operation()
    {
        var owner = await api.CreateSignedInUserAsync();
        var stranger = await api.CreateSignedInUserAsync();
        var food = Assert.Single(await ListAsync(owner), node => node.Name == "Food");
        var ownersHousing = Assert.Single(await ListAsync(owner), node => node.Name == "Housing");
        var strangersHousing = Assert.Single(await ListAsync(stranger), node => node.Name == "Housing");
        var groceries = Assert.Single(food.Children, child => child.Name == "Groceries");
        var url = $"{CategoriesEndpoint}/{groceries.Id}";

        HttpRequestMessage[] Operations(Guid parent) =>
        [
            new(HttpMethod.Put, url + "/name") { Content = JsonContent.Create(new { name = "Mine now" }) },
            new(HttpMethod.Put, url + "/parent") { Content = JsonContent.Create(new { parentId = parent }) },
            new(HttpMethod.Post, url + "/archive"),
            new(HttpMethod.Post, url + "/unarchive"),
            new(HttpMethod.Post, CategoriesEndpoint) { Content = JsonContent.Create(new { name = "Sneaky", parentId = food.Id }) },
        ];

        foreach (var operation in Operations(strangersHousing.Id))
        {
            Assert.Equal(HttpStatusCode.NotFound, (await stranger.Client.SendAsync(operation)).StatusCode);
        }

        // Another User's parent is not found either.
        var intoOwnersParent = await owner.Client.PutAsJsonAsync($"{url}/parent", new { parentId = strangersHousing.Id });
        Assert.Equal(HttpStatusCode.NotFound, intoOwnersParent.StatusCode);

        // The routes exist: the owner can do all of it.
        foreach (var operation in Operations(ownersHousing.Id))
        {
            Assert.True((await owner.Client.SendAsync(operation)).IsSuccessStatusCode);
        }
    }

    private static async Task<List<TreeNode>> ListAsync(SignedInUser user, bool includeArchived = false) =>
        (await user.Client.GetFromJsonAsync<List<TreeNode>>($"{CategoriesEndpoint}?includeArchived={includeArchived}"))!;

    private static int Count(List<TreeNode> tree) => tree.Sum(node => 1 + node.Children.Count);

    private static async Task<CategoryView> CreateAsync(SignedInUser user, object category)
    {
        var created = await user.Client.PostAsJsonAsync(CategoriesEndpoint, category);
        created.EnsureSuccessStatusCode();
        return (await created.Content.ReadFromJsonAsync<CategoryView>())!;
    }

    private sealed record CategoryView(Guid Id, string Name, string Kind, Guid? ParentId, bool IsArchived);

    private sealed record TreeNode(Guid Id, string Name, string Kind, bool IsArchived, List<TreeNode> Children);
}
