using DG.Tweening;
using TMPro;
using UnityEngine;

public class DamagePopup : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _text;

    private static readonly Quaternion SpawnRotation = Quaternion.Euler(90f, 90f, 90f);
    private const float Duration = 2f;
    private const float RiseHeight = 1.5f;

    public void Play(int damage)
    {
        transform.rotation = SpawnRotation;
        _text.text = $"-{damage}";
        _text.alpha = 1f;

        var seq = DOTween.Sequence();
        var target = transform.position;
        target.z += RiseHeight;
        seq.Append(transform.DOMove(target, Duration).SetEase(Ease.OutSine));
        seq.Insert(Duration * 0.5f, _text.DOFade(0f, Duration * 0.5f));
        seq.OnComplete(() => Destroy(gameObject));
    }
}
