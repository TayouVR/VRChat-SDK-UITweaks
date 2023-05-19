# VSUT - VRChat-SDK-UITweaks

[![openupm](https://img.shields.io/npm/v/org.tayou.vrchat.sdk-ui-tweaks?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/org.tayou.vrchat.sdk-ui-tweaks/)

This package is a collection of harmony and reflection based patches for the VRChat SDK.
When installing this package (either via OpenUPM or as git package) make sure you have harmony installed too. OpenUPM has the "EditorPatching" package, which includes harmony.

## Installation
#### OpenUPM (easiest)
- follow the instructions in the top right [of the OpenUPM page for this package](https://openupm.com/packages/org.tayou.vrchat.sdk-ui-tweaks/)

#### as git package
1. make sure you have git installed on your system and registered in the $PATH
2. copy the git URL<br> ![image](https://user-images.githubusercontent.com/31988415/236935196-0c6dc425-e7ea-4f3e-92d3-accdbec582f8.png)
3. paste it in the UPM UI in unity under "Add Git package"<br> ![image](https://user-images.githubusercontent.com/31988415/236935593-7ce9ac50-9a78-4c41-a123-f9d2977db9b4.png)
4. download the newest version of Harmony from https://github.com/pardeike/Harmony/releases and extract 0Harmony.dll from the 4.7.2 folder into your unity assets folder 

#### manual install
1. download the entire repository as zip and extract it into your projects **Packages** folder, or clone it into said folder. (**NOT `Assets`, `Packages`**)
2. download the newest version of Harmony from https://github.com/pardeike/Harmony/releases and extract 0Harmony.dll from the 4.7.2 folder into your unity assets folder

## Patches

- ExpressionParameters list uses a ReorderableList
- ExpressionMenu list uses a ReorderableList
- ParameterDriver list uses a ReorderableList
- ExpressionParameters asset can transfer parameters to a AnimatorController easily
- disable upload restrictions (this is because I'm too lazy to add my EditorOnly objects to VRChats whitelist, its just to allow uploading. The components are still stripped.)

want anything else patched? write an issue or a PR. 
I will not accept any contributions, that are of malicious intention. (that includes *actually breaking* vrchats script stripping or similar things) 
