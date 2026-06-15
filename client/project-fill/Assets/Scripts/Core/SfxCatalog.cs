using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Core
{
    public enum SfxId
    {
        ConfirmClick,
        CancelClick,
        RewardClaimed,
        StageClear,
        StageFail,
        CellGroupRemoved,
        BoardRotated,
        ToastError,
        ItemUsed,
        ActionBlocked,
        GravityLand
    }

    [Serializable]
    public sealed class SfxEntry
    {
        public SfxId id;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
        public Vector2 pitchRange = Vector2.one;
        public float cooldownSeconds;
    }

    [CreateAssetMenu(menuName = "ProjectFill/Audio/SFX Catalog")]
    public sealed class SfxCatalog : ScriptableObject
    {
        [SerializeField] List<SfxEntry> entries = new();

        public bool TryGet(SfxId id, out SfxEntry entry)
        {
            foreach (var candidate in entries)
            {
                if (candidate != null && candidate.id == id)
                {
                    entry = candidate;
                    return true;
                }
            }

            entry = null;
            return false;
        }
    }

    public static class SfxEventBus
    {
        public static event Action<SfxId> Requested;

        public static void Play(SfxId id) => Requested?.Invoke(id);
    }
}
