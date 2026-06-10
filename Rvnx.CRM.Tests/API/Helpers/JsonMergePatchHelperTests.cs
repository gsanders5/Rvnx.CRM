using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.API.Helpers;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Rvnx.CRM.Tests.API.Helpers;

public class JsonMergePatchHelperTests
{
    private sealed class PatchTarget
    {
        public Guid Id { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public int Count { get; set; }
    }

    private sealed class ValidatedPatchTarget
    {
        public Guid Id { get; set; }

        [Required]
        public string? Name { get; set; }

        [StringLength(5)]
        public string? Code { get; set; }
    }

    private static JsonElement ParseJson(string json)
    {
        return JsonSerializer.Deserialize<JsonElement>(json);
    }

    [Fact]
    public void ApplyPatchShouldOverwriteOnlyPropertiesPresentInPatch()
    {
        PatchTarget target = new()
        { FirstName = "Ada", LastName = "Lovelace", Count = 7 };

        JsonMergePatchHelper.ApplyPatch(target, ParseJson("""{"firstName":"Grace"}"""));

        Assert.Equal("Grace", target.FirstName);
        Assert.Equal("Lovelace", target.LastName);
        Assert.Equal(7, target.Count);
    }

    [Fact]
    public void ApplyPatchShouldIgnoreIdProperty()
    {
        Guid originalId = Guid.NewGuid();
        PatchTarget target = new()
        { Id = originalId };

        JsonMergePatchHelper.ApplyPatch(target, ParseJson($$"""{"id":"{{Guid.NewGuid()}}"}"""));

        Assert.Equal(originalId, target.Id);
    }

    [Fact]
    public void ApplyPatchShouldMatchPropertyNamesCaseInsensitively()
    {
        PatchTarget target = new();

        JsonMergePatchHelper.ApplyPatch(target, ParseJson("""{"FIRSTNAME":"Grace","lastname":"Hopper"}"""));

        Assert.Equal("Grace", target.FirstName);
        Assert.Equal("Hopper", target.LastName);
    }

    [Fact]
    public void ApplyPatchShouldSetPropertyToNullWhenPatchValueIsNull()
    {
        PatchTarget target = new()
        { FirstName = "Ada" };

        JsonMergePatchHelper.ApplyPatch(target, ParseJson("""{"firstName":null}"""));

        Assert.Null(target.FirstName);
    }

    [Fact]
    public void ApplyPatchShouldThrowArgumentExceptionWhenPatchIsNotObject()
    {
        PatchTarget target = new();

        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => JsonMergePatchHelper.ApplyPatch(target, ParseJson("""["not","an","object"]""")));

        Assert.Equal("Patch body must be a JSON object.", exception.Message);
    }

    [Fact]
    public void ApplyPatchShouldIgnoreUnknownProperties()
    {
        PatchTarget target = new()
        { FirstName = "Ada" };

        JsonMergePatchHelper.ApplyPatch(target, ParseJson("""{"doesNotExist":42,"firstName":"Grace"}"""));

        Assert.Equal("Grace", target.FirstName);
    }

    [Fact]
    public void ApplyPatchShouldReturnSameTargetInstance()
    {
        PatchTarget target = new();

        PatchTarget result = JsonMergePatchHelper.ApplyPatch(target, ParseJson("{}"));

        Assert.Same(target, result);
    }

    [Fact]
    public void ApplyAndValidateShouldReturnBadRequestWhenPatchedObjectIsInvalid()
    {
        ValidatedPatchTarget target = new()
        { Name = "Valid" };

        IActionResult? result = JsonMergePatchHelper.ApplyAndValidate(target, ParseJson("""{"name":null,"code":"toolong"}"""));

        BadRequestObjectResult badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
    }

    [Fact]
    public void ApplyAndValidateShouldReturnNullWhenPatchedObjectIsValid()
    {
        ValidatedPatchTarget target = new()
        { Name = "Valid" };

        IActionResult? result = JsonMergePatchHelper.ApplyAndValidate(target, ParseJson("""{"code":"ok"}"""));

        Assert.Null(result);
        Assert.Equal("ok", target.Code);
    }
}
