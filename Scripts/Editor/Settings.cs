using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tayou.VRChat.SDKUITweaks.Editor {
    public class Settings {
        
        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider() {
            return new SettingsProvider("Project/VRChat SDK UI Tweaks", SettingsScope.Project) {
                label = "VRChat SDK UI Tweaks",
                activateHandler  = (searchContext, rootElement) => {
                    var settings = Patches.Prefs;

                    Label headerLabel = new Label("VRChat SDK UI Tweaks");
                    headerLabel.style.fontSize = new StyleLength(new Length(20, LengthUnit.Pixel));
                    headerLabel.style.unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Bold);
                    rootElement.Add(headerLabel);

                    Label disclaimerLabel = new Label("Disclaimer: Restart Unity after toggling patches, " +
                                                      "while the code could handle live patching and unpatching technically, " +
                                                      "I have not hooked up the necessary logic for it.");
                    disclaimerLabel.style.whiteSpace = new StyleEnum<WhiteSpace>(WhiteSpace.Normal);
                    rootElement.Add(disclaimerLabel);
                    
                    Label listHeaderLabel = new Label("Features:");
                    rootElement.Add(listHeaderLabel);
                    
                    foreach (var patchProperties in settings.PatchProperties) {
                        var toggle = new Toggle(patchProperties.displayName);
                        toggle.value = patchProperties.isEnabled;
                        toggle.RegisterValueChangedCallback(changeEvent => {
                            settings.PatchProperties.First(pp => pp.displayName == patchProperties.displayName).isEnabled = changeEvent.newValue;
                            settings.Save();
                        });
                        rootElement.Add(toggle);
                    }
                    settings.Save();
                    // GUI code here
                },
                keywords = new HashSet<string>(new[] { "VRChat", "SDK", "UI", "Tweaks", "VSUT" })
            };
        }
    }
}