#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class PlayerPrefsResetMenu
{
#if UNITY_EDITOR
    [MenuItem("Tools/Reset PlayerPrefs")]
    public static void ResetPrefs()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("PlayerPrefs 초기화 완료");
    }
#endif
}
