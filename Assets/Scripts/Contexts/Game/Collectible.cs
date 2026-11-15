using PrimeTween;
using Project.Core.Events;
using Project.Core.Pooling;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Project.Contexts.Game
{
    /// <summary>
    /// Pooled sample object. Pops in on spawn, reports a tap through the event bus and
    /// expires after <see cref="_lifetime"/> seconds. It never destroys itself: the controller despawns it.
    /// Taps arrive through the camera's Physics2DRaycaster, so a Collider2D is required.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class Collectible : MonoBehaviour, IPoolable, IPointerClickHandler
    {
        [SerializeField] private float _lifetime = 2.5f;

        private float _age;
        private bool _live;

        public void OnSpawn()
        {
            _age = 0f;
            _live = true;
            transform.localScale = Vector3.zero;
            PrimeTween.Tween.Scale(transform, 1f, 0.2f, Ease.OutBack);
        }

        public void OnDespawn()
        {
            _live = false;
            PrimeTween.Tween.StopAll(transform);
        }

        private void Update()
        {
            if (!_live) return;
            _age += Time.deltaTime;
            if (_age < _lifetime) return;
            _live = false;
            EventBus<CollectibleExpired>.Raise(new CollectibleExpired(this));
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!_live) return;
            _live = false;
            EventBus<CollectibleTapped>.Raise(new CollectibleTapped(this));
        }
    }
}
