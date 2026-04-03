using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SwiftRunner
{
    public sealed class SwiftRunnerHud : MonoBehaviour
    {
        private TextMeshProUGUI comboText;
        private TextMeshProUGUI phaseText;
        private TextMeshProUGUI statusText;
        private TextMeshProUGUI hintText;
        private TextMeshProUGUI controlsText;
        private TextMeshProUGUI pursuitText;
        private Image pursuitFill;

        public void Initialize()
        {
            var topLeftPanel = FindOrCreatePanel("TopLeftPanel", new Vector2(0f, 1f), new Vector2(22f, -18f), new Vector2(460f, 120f));
            var topCenterPanel = FindOrCreatePanel("TopCenterPanel", new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(900f, 80f));
            var topRightPanel = FindOrCreatePanel("TopRightPanel", new Vector2(1f, 1f), new Vector2(-24f, -18f), new Vector2(360f, 120f));
            var bottomHintPanel = FindOrCreatePanel("BottomHintPanel", new Vector2(0.5f, 0f), new Vector2(0f, 46f), new Vector2(900f, 80f));

            comboText = FindOrCreateLabel(topLeftPanel, "Combo", Vector2.zero, 34f, TextAlignmentOptions.TopLeft);
            phaseText = FindOrCreateLabel(topLeftPanel, "Phase", new Vector2(0f, -44f), 26f, TextAlignmentOptions.TopLeft);
            statusText = FindOrCreateLabel(topCenterPanel, "Status", Vector2.zero, 30f, TextAlignmentOptions.Top);
            controlsText = FindOrCreateLabel(topCenterPanel, "Controls", new Vector2(0f, -34f), 18f, TextAlignmentOptions.Top);
            pursuitText = FindOrCreateLabel(topRightPanel, "PursuitLabel", Vector2.zero, 22f, TextAlignmentOptions.TopRight);
            hintText = FindOrCreateLabel(bottomHintPanel, "Hint", Vector2.zero, 24f, TextAlignmentOptions.Bottom);
            pursuitFill = FindOrCreatePursuitBar(topRightPanel);
            statusText.alpha = 0.9f;
            controlsText.alpha = 0.92f;
        }

        public void Refresh(int combo, int score, string phase, string hint, string controls, float pursuitProgress)
        {
            if (comboText != null)
            {
                comboText.text = $"Combo {combo}\nScore {score}";
            }

            if (phaseText != null)
            {
                phaseText.text = phase;
            }

            if (hintText != null)
            {
                hintText.text = hint;
            }

            if (controlsText != null)
            {
                controlsText.text = controls;
            }

            if (pursuitText != null)
            {
                pursuitText.text = pursuitProgress > 0.001f ? "追兵压力" : "追兵安全";
            }

            if (pursuitFill != null)
            {
                pursuitFill.fillAmount = Mathf.Clamp01(pursuitProgress);
                pursuitFill.color = Color.Lerp(new Color(0.3f, 0.9f, 0.45f, 0.95f), new Color(1f, 0.3f, 0.24f, 0.95f), pursuitProgress);
            }
        }

        public void FlashEvent(string message, Color color)
        {
            if (statusText == null)
            {
                return;
            }

            statusText.color = color;
            statusText.text = message;
        }

        public void ShowStatus(string message, Color color)
        {
            if (statusText == null)
            {
                return;
            }

            statusText.color = color;
            statusText.text = message;
        }

        private RectTransform FindOrCreatePanel(string objectName, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
        {
            var existing = transform.Find(objectName);
            if (existing != null)
            {
                var existingPanel = existing.GetComponent<RectTransform>();
                if (existingPanel != null)
                {
                    existingPanel.anchorMin = anchor;
                    existingPanel.anchorMax = anchor;
                    existingPanel.pivot = anchor;
                    existingPanel.anchoredPosition = anchoredPosition;
                    existingPanel.sizeDelta = size;
                    return existingPanel;
                }
            }

            var panelObject = new GameObject(objectName);
            panelObject.transform.SetParent(transform, false);

            var rectTransform = panelObject.AddComponent<RectTransform>();
            rectTransform.anchorMin = anchor;
            rectTransform.anchorMax = anchor;
            rectTransform.pivot = anchor;
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = size;
            return rectTransform;
        }

        private TextMeshProUGUI FindOrCreateLabel(Transform parent, string objectName, Vector2 anchoredPosition, float fontSize, TextAlignmentOptions alignment)
        {
            var existing = parent.Find(objectName);
            if (existing != null)
            {
                var existingText = existing.GetComponent<TextMeshProUGUI>();
                if (existingText != null)
                {
                    var existingRect = existingText.rectTransform;
                    existingRect.anchorMin = alignment == TextAlignmentOptions.TopLeft ? new Vector2(0f, 1f) :
                        alignment == TextAlignmentOptions.Top ? new Vector2(0.5f, 1f) :
                        new Vector2(0.5f, 0f);
                    existingRect.anchorMax = existingRect.anchorMin;
                    existingRect.pivot = existingRect.anchorMin;
                    existingRect.anchoredPosition = anchoredPosition;
                    existingRect.sizeDelta = new Vector2(900f, 140f);
                    existingText.font = TMP_Settings.defaultFontAsset;
                    existingText.fontSize = fontSize;
                    existingText.alignment = alignment;
                    existingText.color = Color.white;
                    return existingText;
                }
            }

            return CreateLabel(parent, objectName, anchoredPosition, fontSize, alignment);
        }

        private TextMeshProUGUI CreateLabel(Transform parent, string objectName, Vector2 anchoredPosition, float fontSize, TextAlignmentOptions alignment)
        {
            var labelObject = new GameObject(objectName);
            labelObject.transform.SetParent(parent, false);

            var rectTransform = labelObject.AddComponent<RectTransform>();
            rectTransform.anchorMin = alignment == TextAlignmentOptions.TopLeft ? new Vector2(0f, 1f) :
                alignment == TextAlignmentOptions.Top ? new Vector2(0.5f, 1f) :
                new Vector2(0.5f, 0f);
            rectTransform.anchorMax = rectTransform.anchorMin;
            rectTransform.pivot = rectTransform.anchorMin;
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = new Vector2(900f, 140f);

            var text = labelObject.AddComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            return text;
        }

        private Image FindOrCreatePursuitBar(RectTransform parent)
        {
            var barRoot = parent.Find("PursuitBar") as RectTransform;
            if (barRoot == null)
            {
                var barObject = new GameObject("PursuitBar");
                barObject.transform.SetParent(parent, false);
                barRoot = barObject.AddComponent<RectTransform>();
            }

            barRoot.anchorMin = new Vector2(1f, 1f);
            barRoot.anchorMax = new Vector2(1f, 1f);
            barRoot.pivot = new Vector2(1f, 1f);
            barRoot.anchoredPosition = new Vector2(0f, -42f);
            barRoot.sizeDelta = new Vector2(260f, 20f);

            var background = EnsureImage(barRoot, "Background", new Color(0f, 0f, 0f, 0.45f), Image.Type.Sliced);
            Stretch(background.rectTransform);

            var fill = EnsureImage(barRoot, "Fill", new Color(0.3f, 0.9f, 0.45f, 0.95f), Image.Type.Filled);
            Stretch(fill.rectTransform);
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.fillAmount = 0f;
            return fill;
        }

        private static Image EnsureImage(RectTransform parent, string name, Color color, Image.Type type)
        {
            var existing = parent.Find(name);
            Image image;
            if (existing == null)
            {
                var go = new GameObject(name);
                go.transform.SetParent(parent, false);
                image = go.AddComponent<Image>();
            }
            else
            {
                image = existing.GetComponent<Image>();
                if (image == null)
                {
                    image = existing.gameObject.AddComponent<Image>();
                }
            }

            image.color = color;
            image.type = type;
            return image;
        }

        private static void Stretch(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
    }
}
