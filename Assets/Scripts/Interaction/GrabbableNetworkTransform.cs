using Unity.Netcode.Components;

/// <summary>
/// Owner-authoritative NetworkTransform for carryable props.
///
/// Whoever owns the object simulates it and everyone else receives the result, which is what lets a client
/// carry an object with no round trip. At rest the owner is the server, so dropped cargo settles in one
/// place for everyone.
///
/// This is deliberately not <see cref="ClientNetworkTransform"/>: that one also calls DontDestroyOnLoad,
/// which is fine for the player object but would move scene-placed props out of their own scene.
/// </summary>
public class GrabbableNetworkTransform : NetworkTransform
{
    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }
}
