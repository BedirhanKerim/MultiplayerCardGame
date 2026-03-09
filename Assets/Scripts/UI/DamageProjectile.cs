using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class DamageProjectile : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _text;
    [SerializeField] private DamagePopup _damagePopupPrefab;
    private float _speed = 1.6f;
    private float _arcHeight = 2.5f;

    private Vector3[] _waypoints;
    private string[] _waypointTexts;
    private int _currentWaypoint;
    private int _damage;
    private bool _pierces;
    private Action _onComplete;

    public void Play(int attack, int damage, Vector3 attackPos, Vector3 defensePos, Vector3 hpPos, bool pierces, Action onComplete = null)
    {
        transform.position = attackPos;
        _damage = damage;
        _pierces = pierces;
        _onComplete = onComplete;

        if (pierces)
        {
            _waypoints = new[] { defensePos, hpPos };
            _waypointTexts = new[] { attack.ToString(), damage.ToString() };
        }
        else
        {
            _waypoints = new[] { defensePos };
            _waypointTexts = new[] { attack.ToString() };
        }

        _text.text = _waypointTexts[0];
        _currentWaypoint = 0;
        MoveToNext();
    }

    private void MoveToNext()
    {
        if (_currentWaypoint >= _waypoints.Length)
        {
            if (_pierces && _damagePopupPrefab != null)
            {
                var popup = Instantiate(_damagePopupPrefab, transform.position, Quaternion.identity);
                popup.Play(_damage);
            }
            _onComplete?.Invoke();
            Destroy(gameObject);
            return;
        }

        _text.text = _waypointTexts[_currentWaypoint];

        var start = transform.position;
        var end = _waypoints[_currentWaypoint];
        end.y += 1f;
        float distance = Vector3.Distance(start, end);
        float duration = distance / _speed;
        var mid = (start + end) * 0.5f;
        mid.y += _arcHeight;

        var path = new[] { mid, end };
        transform.DOPath(path, duration, PathType.CatmullRom)
            .SetEase(Ease.InQuad)
            .OnComplete(() =>
            {
                _currentWaypoint++;
                MoveToNext();
            });
    }
}
