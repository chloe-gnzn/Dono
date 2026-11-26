namespace BloodMatch.Web.Models;

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
