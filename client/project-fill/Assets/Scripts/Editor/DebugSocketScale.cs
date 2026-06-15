#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Game.InGame.View;

namespace Game.Editor
{
    public static class DebugSocketScale
    {
        [MenuItem("Tools/UI Setup/Debug Sockets and Cells", false, 151)]
        public static void DebugSocketsAndCells()
        {
            var boardBg = Object.FindAnyObjectByType<BoardBackground>();
            if (boardBg != null)
            {
                Debug.Log($"--- Debugging BoardBackground: {boardBg.name} ---");
                Debug.Log($"BoardBackground localScale: {boardBg.transform.localScale}, worldScale: {boardBg.transform.lossyScale}");
                
                int childCount = boardBg.transform.childCount;
                Debug.Log($"BoardBackground child count: {childCount}");
                for (int i = 0; i < childCount; i++)
                {
                    var child = boardBg.transform.GetChild(i);
                    var sr = child.GetComponent<SpriteRenderer>();
                    if (sr != null && sr.sprite != null)
                    {
                        var sprite = sr.sprite;
                        Debug.Log($"Child [{child.name}]: localScale = {child.localScale}, worldScale = {child.lossyScale}, sprite = {sprite.name}, spriteBounds = {sprite.bounds.size}, ppu = {sprite.pixelsPerUnit}, rect = {sprite.rect}");
                    }
                    else
                    {
                        Debug.Log($"Child [{child.name}]: localScale = {child.localScale}, worldScale = {child.lossyScale}, SpriteRenderer/Sprite is null");
                    }
                }
            }
            else
            {
                Debug.LogWarning("BoardBackground not found in scene!");
            }

            var boardView = Object.FindAnyObjectByType<BoardView>();
            if (boardView != null)
            {
                Debug.Log($"--- Debugging BoardView: {boardView.name} ---");
                Debug.Log($"BoardView localScale: {boardView.transform.localScale}, worldScale: {boardView.transform.lossyScale}");
                
                int childCount = boardView.transform.childCount;
                Debug.Log($"BoardView child count: {childCount}");
                for (int i = 0; i < childCount; i++)
                {
                    var child = boardView.transform.GetChild(i);
                    if (child.name.Contains("Clone") || child.GetComponent<CellView>() != null)
                    {
                        var cv = child.GetComponent<CellView>();
                        var sr = child.GetComponent<SpriteRenderer>();
                        if (sr == null && cv != null)
                        {
                            sr = child.GetComponentInChildren<SpriteRenderer>();
                        }
                        
                        if (sr != null && sr.sprite != null)
                        {
                            var sprite = sr.sprite;
                            Debug.Log($"Cell [{child.name}]: localScale = {child.localScale}, worldScale = {child.lossyScale}, sprite = {sprite.name}, spriteBounds = {sprite.bounds.size}, ppu = {sprite.pixelsPerUnit}, rect = {sprite.rect}");
                        }
                        else
                        {
                            Debug.Log($"Cell [{child.name}]: localScale = {child.localScale}, worldScale = {child.lossyScale}, SpriteRenderer/Sprite is null");
                        }
                    }
                }
            }
            else
            {
                Debug.LogWarning("BoardView not found in scene!");
            }
        }
    }
}
#endif
