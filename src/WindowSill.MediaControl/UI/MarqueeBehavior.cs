namespace WindowSill.MediaControl.UI;

/// <summary>
/// How the Marquee moves.
/// </summary>
public enum MarqueeBehavior
{
    /// <summary>
    /// The text flows across the screen from start to finish.
    /// </summary>
    Ticker,

    /// <summary>
    /// As the text flows across the screen a duplicate follows.
    /// </summary>
    /// <remarks>
    /// Looping text won't move if all the text already fits on the screen.
    /// </remarks>
    Looping,

    // Waiting on AutoReverse implementation for Uno storyboards
    /// <summary>
    /// The text bounces back and forth across the screen.
    /// </summary>
    Bouncing,
}
