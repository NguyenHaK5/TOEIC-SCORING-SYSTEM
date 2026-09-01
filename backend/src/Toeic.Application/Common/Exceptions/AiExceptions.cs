namespace Toeic.Application.Common.Exceptions;

/// <summary>
/// Thrown when the LLM provider call itself fails — network error, timeout,
/// non-2xx HTTP status after internal transport retries, or missing API key
/// configuration. The TOEIC backend itself is not at fault; the upstream AI
/// provider is. Map to HTTP 502 Bad Gateway in ExceptionHandlingMiddleware
/// (manual step — see PHASE3_PART3_INTEGRATION_GUIDE.md).
///
/// ASSUMPTION: extends the same AppException base used since Phase 1 Part 2
/// (single-string-message constructor). Adjust if the real base type differs.
/// </summary>
public class AiProviderException : AppException
{

    public override int StatusCode => 502;

    public AiProviderException(string message) : base(message)
    {
    }
}

/// <summary>
/// Thrown when the LLM responded successfully at the HTTP level, but its
/// content could not be parsed into the required JSON schema or failed
/// rubric-criteria validation — even after AiAssessmentOptions.MaxAttempts
/// retries. Map to HTTP 500. From the API consumer's point of view this
/// means "Writing could not be auto-scored right now"; Phase 4 (Human
/// Rater) can use this as a signal to route the answer for manual grading.
/// </summary>
public class AiOutputValidationException : AppException
{

    public override int StatusCode => 502;

    public AiOutputValidationException(string message) : base(message)
    {
    }
}
