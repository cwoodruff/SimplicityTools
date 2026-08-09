namespace Sample.ClaimsPortal.Claims.Adjudication;

public sealed record AdjudicationRequest(string ClaimNumber, ClaimSubmission Submission);
