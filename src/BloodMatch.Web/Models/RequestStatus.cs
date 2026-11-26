namespace BloodMatch.Web.Models;

/// <summary>Lifecycle of a blood request, from draft to closure.</summary>
public enum RequestStatus
{
    Draft,
    PendingVerification,
    Approved,
    Matching,
    Fulfilled,
    Denied,
    Expired,
    Cancelled
}
