namespace GalleryBetak.Application.Common;

/// <summary>
/// Standard API response envelope wrapping all API responses.
/// Provides consistent structure for success and error scenarios.
/// </summary>
/// <typeparam name="T">The type of the data payload.</typeparam>
public sealed class ApiResponse<T>
{
    /// <summary>
    /// Indicates whether the request was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// HTTP status code of the response.
    /// </summary>
    public int StatusCode { get; init; }

    /// <summary>
    /// Arabic user-facing message.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// English message for developer/debugging purposes.
    /// </summary>
    public string MessageEn { get; init; } = string.Empty;

    /// <summary>
    /// The response data payload. Null on error responses.
    /// </summary>
    public T? Data { get; init; }

    /// <summary>
    /// List of field-level validation errors.
    /// </summary>
    public List<ApiError> Errors { get; init; } = new();

    /// <summary>
    /// Pagination metadata, if applicable.
    /// </summary>
    public PaginationMeta? Meta { get; init; }

    /// <summary>
    /// Creates a successful response with data.
    /// </summary>
    public static ApiResponse<T> Ok(T data, string message = "تمت العملية بنجاح", string messageEn = "Operation completed successfully")
    {
        return new ApiResponse<T>
        {
            Success = true,
            StatusCode = 200,
            Message = message,
            MessageEn = messageEn,
            Data = data
        };
    }

    /// <summary>
    /// Creates a successful response with data and pagination metadata.
    /// </summary>
    public static ApiResponse<T> Ok(T data, PaginationMeta meta, string message = "تم جلب البيانات بنجاح", string messageEn = "Data retrieved successfully")
    {
        return new ApiResponse<T>
        {
            Success = true,
            StatusCode = 200,
            Message = message,
            MessageEn = messageEn,
            Data = data,
            Meta = meta
        };
    }

    /// <summary>
    /// Creates a 201 Created response.
    /// </summary>
    public static ApiResponse<T> Created(T data, string message = "تم الإنشاء بنجاح", string messageEn = "Created successfully")
    {
        return new ApiResponse<T>
        {
            Success = true,
            StatusCode = 201,
            Message = message,
            MessageEn = messageEn,
            Data = data
        };
    }

    /// <summary>
    /// Creates an error response.
    /// </summary>
    public static ApiResponse<T> Fail(int statusCode, string message, string messageEn, List<ApiError>? errors = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            StatusCode = statusCode,
            Message = message,
            MessageEn = messageEn,
            Errors = errors ?? new List<ApiError>()
        };
    }
}

/// <summary>
/// Represents a single field-level validation error.
/// </summary>
public sealed class ApiError
{
    /// <summary>
    /// The field name that caused the error.
    /// </summary>
    public string Field { get; init; } = string.Empty;

    /// <summary>
    /// Arabic error message for the field.
    /// </summary>
    public string Message { get; init; } = string.Empty;
}

/// <summary>
/// Pagination metadata included in paginated API responses.
/// </summary>
public sealed class PaginationMeta
{
    /// <summary>
    /// Current page number (1-indexed).
    /// </summary>
    public int CurrentPage { get; init; }

    /// <summary>
    /// Number of items per page.
    /// </summary>
    public int PageSize { get; init; }

    /// <summary>
    /// Total number of items across all pages.
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// Total number of pages.
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary>
    /// Whether there is a previous page.
    /// </summary>
    public bool HasPrevious => CurrentPage > 1;

    /// <summary>
    /// Whether there is a next page.
    /// </summary>
    public bool HasNext => CurrentPage < TotalPages;
}

/// <summary>
/// Generic paged result returned by service layer queries.
/// </summary>
/// <typeparam name="T">The type of items in the page.</typeparam>
public sealed class PagedResult<T>
{
    /// <summary>
    /// The items in the current page.
    /// </summary>
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();

    /// <summary>
    /// Total number of items across all pages.
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// Current page number.
    /// </summary>
    public int PageNumber { get; init; }

    /// <summary>
    /// Number of items per page.
    /// </summary>
    public int PageSize { get; init; }

    /// <summary>
    /// Creates pagination metadata from this result.
    /// </summary>
    public PaginationMeta ToMeta() => new()
    {
        CurrentPage = PageNumber,
        PageSize = PageSize,
        TotalCount = TotalCount
    };
}

