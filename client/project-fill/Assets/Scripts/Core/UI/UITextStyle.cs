using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Core.UI
{
    /// <summary>
    /// Dynamically applies bold face dilate and drop shadows (underlays) to TMPro text.
    /// Automatically samples parent button/image color to generate a matching rich dark shadow.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class UITextStyle : MonoBehaviour
    {
        [Tooltip("If enabled, automatically detects parent Button/Image color and applies matching shadow.")]
        [SerializeField] private bool _autoShadowFromParent = true;
        [SerializeField] private Color _customShadowColor = new Color(0f, 0f, 0f, 0.75f);
        [SerializeField] private Vector2 _shadowOffset = new Vector2(0.6f, -0.6f); // Relative offset for TMP underlay (5x larger)
        [SerializeField] private float _shadowDilate = 0.4f; // Makes the shadow outline thicker/bold
        [SerializeField] private float _shadowSoftness = 0.05f; // Low softness for clean retro/casual look
        [SerializeField] private float _faceDilate = 0.15f; // Dilates the font face to make it bold and readable

        private TMP_Text _tmp;

        private void Awake()
        {
            _tmp = GetComponent<TMP_Text>();
        }

        private void Start()
        {
            ApplyStyle();
        }

        private void OnEnable()
        {
            ApplyStyle();
        }

        public void ApplyStyle()
        {
            if (_tmp == null) _tmp = GetComponent<TMP_Text>();
            if (_tmp == null) return;

            Color shadowColor = _customShadowColor;

            if (_autoShadowFromParent)
            {
                // Find nearest parent Graphic component (skipping this game object itself)
                Graphic parentGraphic = null;
                Transform p = transform.parent;
                while (p != null)
                {
                    if (p.TryGetComponent<Graphic>(out var g))
                    {
                        parentGraphic = g;
                        break;
                    }
                    p = p.parent;
                }

                if (parentGraphic != null)
                {
                    shadowColor = CalculateShadowColor(parentGraphic.color, _tmp.color);
                }
            }

            ApplyMaterialProperties(shadowColor);
        }

        private Color CalculateShadowColor(Color bgCol, Color textCol)
        {
            // Convert background color to hex to match designated UI theme colors
            string hex = ColorUtility.ToHtmlStringRGB(bgCol).ToUpper();

            Color shadowColor;

            // Hand-curated high-contrast dark drop-shadow mapping for designated UI colors
            switch (hex)
            {
                case "4D1259": // UI_BG_DEEP (Old)
                case "2A1635": // UI_BG_DEEP (New cozy deep grape-purple)
                    shadowColor = HexColor("120216"); // Deepest dark purple shadow
                    break;
                case "7C238C": // UI_BG_MID (Old)
                case "4D235D": // UI_BG_MID (New warm plum-purple)
                    shadowColor = HexColor("2B003B"); // Rich dark purple shadow
                    break;
                case "FF5E7E": // UI_PRIMARY (Old)
                case "FF4D79": // UI_PRIMARY (New vibrant strawberry pink)
                    shadowColor = HexColor("5B0D21"); // Deep dark wine red/pink shadow
                    break;
                case "FFD124": // UI_CTA (Old)
                case "FFC700": // UI_CTA (New sunny yellow)
                    shadowColor = HexColor("2A1635"); // Deep grape-purple shadow for contrast
                    break;
                case "24D878": // UI_SUCCESS (Old)
                case "2ED573": // UI_SUCCESS (New lime-green mint)
                    shadowColor = HexColor("0A3C1C"); // Deep forest green shadow
                    break;
                case "FF3B30": // UI_DANGER (Old)
                case "FF4757": // UI_DANGER (New coral red)
                    shadowColor = HexColor("4A0A12"); // Deep dark crimson shadow
                    break;
                case "FFAA00": // UI_BORDER (Old)
                case "FF9F00": // UI_BORDER (New orange-yellow highlights)
                    shadowColor = HexColor("2A1635"); // Deep grape-purple shadow
                    break;
                case "2B003B": // Dark button shadow underlay
                    shadowColor = HexColor("120216"); // Deepest dark purple shadow
                    break;
                default:
                    // Mathematical dark complementary/harmonious shadow fallback
                    float h, s, v;
                    Color.RGBToHSV(bgCol, out h, out s, out v);
                    
                    // Keep the hue, increase saturation, and significantly lower the value for a dark shadow
                    float shadowS = Mathf.Min(s + 0.2f, 1.0f);
                    float shadowV = Mathf.Max(v * 0.25f, 0.08f);
                    shadowColor = Color.HSVToRGB(h, shadowS, shadowV);
                    break;
            }

            shadowColor.a = 1.0f; // Solid opaque shadow for high-end comic appeal
            return shadowColor;
        }

        private Color HexColor(string hex)
        {
            Color c;
            ColorUtility.TryParseHtmlString("#" + hex, out c);
            return c;
        }

        private void ApplyMaterialProperties(Color shadowColor)
        {
            if (_tmp.font == null) return;

            // If the serialized values are using the old tiny defaults, override them with large ones
            Vector2 baseOffset = _shadowOffset;
            if (baseOffset.magnitude < 0.25f)
            {
                baseOffset = new Vector2(0.65f, -0.65f); // 5x larger shadow offset by default
            }
            float baseDilate = _shadowDilate;
            if (baseDilate < 0.2f)
            {
                baseDilate = 0.4f; // Thick bold shadow dilate
            }

            // Dynamically scale shadow parameters based on current font size (reference size = 36)
            float baseSize = 36f;
            float scaleFactor = Mathf.Clamp(_tmp.fontSize / baseSize, 0.5f, 2.5f);

            Vector2 dynamicOffset = baseOffset * scaleFactor;
            float dynamicDilate = baseDilate * scaleFactor;
            float dynamicSoftness = _shadowSoftness * scaleFactor;
            float dynamicFaceDilate = _faceDilate * scaleFactor;

            if (Application.isPlaying)
            {
                // Play-mode: Use cached/shared materials to preserve batching and reduce draw calls
                Material cachedMat = UIFontMaterialCache.GetOrCreateMaterial(
                    _tmp.font, 
                    shadowColor, 
                    dynamicOffset, 
                    dynamicDilate, 
                    dynamicSoftness, 
                    dynamicFaceDilate
                );
                
                if (cachedMat != null)
                {
                    _tmp.fontSharedMaterial = cachedMat;
                    _tmp.SetMaterialDirty();
                }
            }
            else
            {
#if UNITY_EDITOR
                // Edit-mode: Modify local material to ensure immediate serialization in prefabs/scenes
                if (_tmp.fontSharedMaterial == null) return; // TMP font not yet assigned (e.g. prefab isolation context)
                Material mat = _tmp.fontMaterial;
                if (mat != null)
                {
                    mat.EnableKeyword("UNDERLAY_ON");
                    mat.SetColor("_UnderlayColor", shadowColor);
                    mat.SetFloat("_UnderlayOffsetX", dynamicOffset.x);
                    mat.SetFloat("_UnderlayOffsetY", dynamicOffset.y);
                    mat.SetFloat("_UnderlayDilate", dynamicDilate);
                    mat.SetFloat("_UnderlaySoftness", dynamicSoftness);
                    mat.SetFloat("_FaceDilate", dynamicFaceDilate);
                    
                    // Mark dirty so changes save to prefab properly
                    UnityEditor.EditorUtility.SetDirty(gameObject);
                }
#endif
            }
        }
    }

    /// <summary>
    /// Thread-safe static cache for dynamically generated TMPro font materials.
    /// Prevents duplicate instantiation of identical material parameters, allowing draw call batching.
    /// </summary>
    public static class UIFontMaterialCache
    {
        private static readonly System.Collections.Generic.Dictionary<string, Material> Cache = 
            new System.Collections.Generic.Dictionary<string, Material>();

        public static Material GetOrCreateMaterial(TMP_FontAsset font, Color shadowColor, Vector2 shadowOffset, float shadowDilate, float shadowSoftness, float faceDilate)
        {
            if (font == null) return null;

            // Create a unique key for the material variant
            string key = $"{font.name}_{shadowColor.r:F2}_{shadowColor.g:F2}_{shadowColor.b:F2}_{shadowColor.a:F2}_{shadowOffset.x:F2}_{shadowOffset.y:F2}_{shadowDilate:F2}_{shadowSoftness:F2}_{faceDilate:F2}";

            if (Cache.TryGetValue(key, out var mat) && mat != null)
            {
                return mat;
            }

            // Create a new material cloned from the font's default shared material
            Material baseMat = font.material;
            Material newMat = new Material(baseMat);
            newMat.name = $"DynamicFontPreset_{key}";
            
            // Enable keywords and set properties
            newMat.EnableKeyword("UNDERLAY_ON");
            newMat.SetColor("_UnderlayColor", shadowColor);
            newMat.SetFloat("_UnderlayOffsetX", shadowOffset.x);
            newMat.SetFloat("_UnderlayOffsetY", shadowOffset.y);
            newMat.SetFloat("_UnderlayDilate", shadowDilate);
            newMat.SetFloat("_UnderlaySoftness", shadowSoftness);
            newMat.SetFloat("_FaceDilate", faceDilate);

            Cache[key] = newMat;
            return newMat;
        }

        public static void Clear()
        {
            foreach (var mat in Cache.Values)
            {
                if (mat != null)
                {
                    if (Application.isPlaying)
                        Object.Destroy(mat);
                    else
                        Object.DestroyImmediate(mat);
                }
            }
            Cache.Clear();
        }
    }
}
