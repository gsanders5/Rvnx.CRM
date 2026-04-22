using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.DTOs.Base;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Models;

namespace Rvnx.CRM.API.Helpers;

/// <summary>
/// Shared mappings from service-layer result types to <see cref="IActionResult"/>.
/// Centralises the HTTP status conventions so every controller responds consistently.
/// </summary>
internal static class ApiResults
{
    /// <summary>
    /// Maps an <see cref="OperationResult"/> to an update/delete-style response:
    /// 204 No Content on success, 400 Bad Request (with <c>{ Error }</c>) otherwise.
    /// </summary>
    public static IActionResult ToNoContentResult(this OperationResult result)
    {
        return result.Success
            ? new NoContentResult()
            : new BadRequestObjectResult(new { Error = result.ErrorMessage });
    }

    /// <summary>
    /// Maps an <see cref="OperationResult"/> to a create-style response:
    /// 200 OK with <c>{ Id }</c> on success, 400 Bad Request otherwise.
    /// </summary>
    public static IActionResult ToCreatedResult(this OperationResult result)
    {
        return result.Success
            ? new OkObjectResult(new { Id = result.RedirectId })
            : new BadRequestObjectResult(new { Error = result.ErrorMessage });
    }

    /// <summary>
    /// Maps a <see cref="ContactOperationResult"/> to 204/404/400 with the <c>{ Errors }</c> payload convention.
    /// </summary>
    public static IActionResult ToNoContentResult(this ContactOperationResult result)
        => ErrorsResult(result.Success, result.IsNotFound, result.Errors);

    /// <summary>
    /// Maps a <see cref="LabelOperationResult"/> to 204/404/400 with the <c>{ Errors }</c> payload convention.
    /// </summary>
    public static IActionResult ToNoContentResult(this LabelOperationResult result)
        => ErrorsResult(result.Success, result.IsNotFound, result.Errors);

    /// <summary>
    /// Maps an <see cref="AttachmentOperationResult"/> to 204/404/400 with the <c>{ Errors }</c> payload convention.
    /// </summary>
    public static IActionResult ToNoContentResult(this AttachmentOperationResult result)
        => ErrorsResult(result.Success, result.IsNotFound, result.Errors);

    private static IActionResult ErrorsResult(bool success, bool isNotFound, List<string> errors)
    {
        if (success)
        {
            return new NoContentResult();
        }

        return isNotFound
            ? new NotFoundResult()
            : new BadRequestObjectResult(new { Errors = errors });
    }
}
