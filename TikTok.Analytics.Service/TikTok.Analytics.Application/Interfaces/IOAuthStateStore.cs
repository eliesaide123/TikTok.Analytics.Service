namespace TikTok.Analytics.Application.Interfaces;

/// <summary>
/// Issues and redeems the OAuth <c>state</c> value that protects the callback against CSRF.
/// A state is single-use: redeeming it removes it.
///
/// The state also carries the PageId being authorized. TikTok's callback only tells us the
/// OpenId, which by itself cannot be mapped back to a configured page, so the association
/// has to be remembered across the round trip.
/// </summary>
public interface IOAuthStateStore
{
    string Issue(string? pageId = null);

    bool TryRedeem(string state, out string? pageId);
}
