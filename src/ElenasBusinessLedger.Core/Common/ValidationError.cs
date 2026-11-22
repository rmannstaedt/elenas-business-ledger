namespace ElenasBusinessLedger.Core.Common;

// Pairs a stable error code with a user-readable explanation for callers that display or branch on an error.
public sealed record ValidationError(string Code, string Message);
