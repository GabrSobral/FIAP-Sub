namespace FiapSub.Sales.Application.Common;

/// <summary>How an expected failure should be reported to the caller.</summary>
public enum ErrorType
{
    Validation,
    NotFound,
    Conflict,
}
