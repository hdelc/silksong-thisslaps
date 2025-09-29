using HutongGames.PlayMaker;
using UnityEngine;

namespace ThisSlaps;

public class EnableColliders : FsmStateAction
{
    public FsmOwnerDefault? gameObject;
    public bool enabled = true;
    public float delay = 0f;

    private GameObject? _gameObject;
    private Collider2D[]? _colliders;
    private float _timer;

    public override void OnEnter()
    {
        _gameObject = base.Fsm?.GetOwnerDefaultTarget(gameObject);
        _colliders = _gameObject?.GetComponents<Collider2D>();
        if (_gameObject is null || _colliders is null)
        {
            Finish();
            return;
        }

        if (delay <= 0f)
        {
            DoColliderUpdate();
            Finish();
        }
        else
        {
            _timer = delay;
        }
    }

    public override void OnUpdate()
    {
        if (_timer > 0f)
        {
            _timer -= Time.deltaTime;
        }
        else
        {
            DoColliderUpdate();
        }
    }

    public override void Reset()
    {
        _gameObject = null;
        _colliders = null;
        _timer = 0f;
    }

    private void DoColliderUpdate()
    {
        foreach (var collider in _colliders)
        {
            collider.enabled = enabled;
        }
        Finish();
    }
}