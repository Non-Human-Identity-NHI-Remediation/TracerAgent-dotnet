namespace TracerAgent.Core.Interfaces;

using TracerAgent.Core.Models;

/// <summary>
/// Dep 1 — Activity Verification.
/// Executes 2-step fallback: SIEM → LDAP.
/// Produces confidence level (High/Medium/Low).
/// </summary>
public interface IActivityVerifier
{
    Task<ActivityVerificationResult> VerifyAsync(
        NHIAccount account,
        CancellationToken ct = default);
}