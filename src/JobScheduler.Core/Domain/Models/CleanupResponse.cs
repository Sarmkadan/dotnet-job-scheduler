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
    public int DeletedCount { get; set; }
    public DateTime CutoffDate { get; set; }
    public string Message { get; set; } = string.Empty;
}
