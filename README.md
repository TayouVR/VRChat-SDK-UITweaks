# VSUT - VRChat-SDK-UITweaks

This package is a collection of harmony and reflection based patches for the VRChat SDK.
When installing this package (either via OpenUPM or as git package) make sure you have harmony installed too. OpenUPM has the "EditorPatching" package, which includes harmony.

## Patches

- ExpressionParameters list uses a ReorderableList
- ExpressionMenu list uses a ReorderableList
- ParameterDriver list uses a ReorderableList
- ExpressionParameters asset can transfer parameters to a AnimatorController easily
- disable upload restrictions (this is because I'm too lazy to add my EditorOnly objects to VRChats whitelist, its just to allow uploading. The components are still stripped.)

want anything else patched? write an issue or a PR. 
I will not accept any contributions, that are of malicious intention. (that includes *actually breaking* vrchats script stripping or similar things) 