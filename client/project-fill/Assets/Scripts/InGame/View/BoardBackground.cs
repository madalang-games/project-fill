using UnityEngine;

namespace Game.InGame.View
{
    public class BoardBackground : MonoBehaviour
    {
        [SerializeField] private ScriptableObject[] _themes;
        [SerializeField] private Color _boardColor     = new(0.118f, 0.118f, 0.18f,  1f);
        [SerializeField] private Color _socketColor    = new(0.165f, 0.165f, 0.243f, 1f);
        [SerializeField] private Color _socketHighlight= new(0.216f, 0.216f, 0.314f, 1f);
        [SerializeField] private Color _socketShadow   = new(0.098f, 0.098f, 0.157f, 1f);
        [SerializeField] private Color _neonCyan       = new(0.15f,  0.95f,  1f,     1f);
        [SerializeField] private Color _neonPink       = new(1f,     0.22f,  0.78f,  1f);
        [SerializeField] private bool  _animateTexture = true;
        [SerializeField] private int   _effectFps      = 12;
        [SerializeField] private Sprite[] _socketSprites;
        [SerializeField] private Sprite   _defaultSocketSprite;
    }
}
