namespace JobScheduler.Core.Controllers;

/// <summary>
/// Provides extension methods for working with paginated responses.
/// </summary>
public static class PaginatedResponseExtensions
{
    /// <summary>
    /// Calculates the total number of pages represented by the paginated response.
    /// </summary>
    /// <typeparam name="T">The type of item contained in the paginated response.</typeparam>
    /// <param name="response">The paginated response whose total page count is calculated.</param>
    /// <returns>
    /// The total number of pages, rounded up to include a partially filled page; or <c>0</c>
    /// when the page size is not positive.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="response"/> is <see langword="null"/>.</exception>
    public static int GetTotalPages<T>(this PaginatedResponse<T> response)
    {
        ArgumentNullException.ThrowIfNull(response);

        return response.PageSize > 0
            ? (int)Math.Ceiling((double)response.TotalCount / response.PageSize)
            : 0;
    }
}
