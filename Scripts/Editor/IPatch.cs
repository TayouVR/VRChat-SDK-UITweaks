using HarmonyLib;

namespace Tayou.VRChat.SDKUITweaks.Editor {
    public interface IPatch {
        void Apply(Harmony harmony);
        void Remove(Harmony harmony);
    }
}