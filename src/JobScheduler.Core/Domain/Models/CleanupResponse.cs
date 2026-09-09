#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;

namespace JobScheduler.Core.Domain.Models;

/// <summary>
/// Response model for cleanup operations.
/// Contains information about deleted items and cutoff date.
/// </summary>
public sealed class CleanupResponse
{
    /// <summary>
    /// Number of deleted items.
    /// </summary>
    public int DeletedCount { get; set; }

    /// <summary>
    /// The cutoff date used for the cleanup operation.
    /// </summary>
    public DateTime CutoffDate { get; set; }

    /// <summary>
    /// A message describing the result of the cleanup operation.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Returns a string representation of the CleanupResponse.
    /// </summary>
    /// <returns>
    /// A string in the format: CleanupResponse { DeletedCount = ..., CutoffDate = ... (ISO 8601 'O' format), Message = ... }
    /// </returns>
    public override string ToString()
    {
        return $"CleanupResponse {{ DeletedCount = {DeletedCount}, CutoffDate = {CutoffDate:O}, Message = {Message} }}";
    }
}