using Project.Core.Events;

namespace Project.Contexts.Game
{
    public enum GameState
    {
        Playing,
        Won,
        Lost
    }

    public readonly struct CollectibleTapped : IEvent
    {
        public readonly Collectible Target;
        public CollectibleTapped(Collectible target) => Target = target;
    }

    public readonly struct CollectibleExpired : IEvent
    {
        public readonly Collectible Target;
        public CollectibleExpired(Collectible target) => Target = target;
    }
}
