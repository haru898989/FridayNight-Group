using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class GoalPresentationUI : MonoBehaviour
{
    private sealed class ConfettiPiece
    {
        public RectTransform rectTransform;
        public float delay;
        public float flightDuration;
        public float startX;
        public float horizontalDrift;
        public float rotationSpeed;
        public float scale;
    }

    [SerializeField] private GameObject celebrationRoot;
    [SerializeField] private GameObject spectatorRoot;
    [SerializeField] private RectTransform confettiLayer;
    [SerializeField] private RectTransform confettiTemplate;
    [SerializeField] private float celebrationDuration = 2f;
    [SerializeField] private int confettiCount = 56;

    private readonly List<ConfettiPiece> confettiPieces = new List<ConfettiPiece>();
    private Coroutine confettiCoroutine;

    public static GoalPresentationUI Instance { get; private set; }
    public float CelebrationDuration => Mathf.Max(0.1f, celebrationDuration);

    private void Awake()
    {
        Instance = this;
        HideAll();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void PlayGoalCelebration()
    {
        if (spectatorRoot != null)
        {
            spectatorRoot.SetActive(false);
        }

        if (celebrationRoot != null)
        {
            celebrationRoot.SetActive(true);
        }

        ClearConfetti();

        if (confettiLayer != null && confettiTemplate != null)
        {
            confettiCoroutine = StartCoroutine(AnimateConfetti());
        }
    }

    public void ShowSpectatorFrame()
    {
        if (celebrationRoot != null)
        {
            celebrationRoot.SetActive(false);
        }

        if (spectatorRoot != null)
        {
            spectatorRoot.SetActive(true);
        }
    }

    public void HideAll()
    {
        ClearConfetti();

        if (celebrationRoot != null)
        {
            celebrationRoot.SetActive(false);
        }

        if (spectatorRoot != null)
        {
            spectatorRoot.SetActive(false);
        }
    }

    private IEnumerator AnimateConfetti()
    {
        float width = Mathf.Max(confettiLayer.rect.width, Screen.width);
        float height = Mathf.Max(confettiLayer.rect.height, Screen.height);
        Color[] colors =
        {
            new Color(1f, 0.25f, 0.35f),
            new Color(1f, 0.75f, 0.15f),
            new Color(0.25f, 0.8f, 1f),
            new Color(0.45f, 1f, 0.45f),
            new Color(0.85f, 0.4f, 1f),
            new Color(1f, 0.55f, 0.8f)
        };

        for (int i = 0; i < confettiCount; i++)
        {
            RectTransform piece = Instantiate(confettiTemplate, confettiLayer);
            piece.name = $"Confetti_{i + 1}";
            piece.gameObject.SetActive(false);

            float pieceScale = Random.Range(0.7f, 1.35f);
            piece.sizeDelta = new Vector2(Random.Range(8f, 18f), Random.Range(16f, 30f));

            Image image = piece.GetComponent<Image>();
            if (image != null)
            {
                image.color = colors[Random.Range(0, colors.Length)];
            }

            confettiPieces.Add(new ConfettiPiece
            {
                rectTransform = piece,
                delay = Random.Range(0f, 0.5f),
                flightDuration = Random.Range(1.05f, 1.55f),
                startX = Random.Range(-width * 0.45f, width * 0.45f),
                horizontalDrift = Random.Range(-width * 0.18f, width * 0.18f),
                rotationSpeed = Random.Range(-520f, 520f),
                scale = pieceScale
            });
        }

        float elapsed = 0f;
        while (elapsed < CelebrationDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            for (int i = 0; i < confettiPieces.Count; i++)
            {
                ConfettiPiece piece = confettiPieces[i];
                float normalizedTime = (elapsed - piece.delay) / piece.flightDuration;

                if (normalizedTime < 0f)
                {
                    continue;
                }

                if (normalizedTime >= 1f)
                {
                    piece.rectTransform.gameObject.SetActive(false);
                    continue;
                }

                if (!piece.rectTransform.gameObject.activeSelf)
                {
                    piece.rectTransform.gameObject.SetActive(true);
                }

                float wave = Mathf.Sin(normalizedTime * Mathf.PI * 4f) * 35f;
                float x = piece.startX + piece.horizontalDrift * normalizedTime + wave;
                float y = Mathf.Lerp(-height * 0.55f, height * 0.6f, normalizedTime);
                piece.rectTransform.anchoredPosition = new Vector2(x, y);
                piece.rectTransform.localRotation = Quaternion.Euler(
                    0f,
                    normalizedTime * 360f,
                    normalizedTime * piece.rotationSpeed
                );
                piece.rectTransform.localScale = Vector3.one * piece.scale;
            }

            yield return null;
        }

        ClearConfetti(false);
        confettiCoroutine = null;
    }

    private void ClearConfetti(bool stopCoroutine = true)
    {
        if (stopCoroutine && confettiCoroutine != null)
        {
            StopCoroutine(confettiCoroutine);
            confettiCoroutine = null;
        }

        for (int i = 0; i < confettiPieces.Count; i++)
        {
            if (confettiPieces[i].rectTransform != null)
            {
                Destroy(confettiPieces[i].rectTransform.gameObject);
            }
        }

        confettiPieces.Clear();
    }
}
