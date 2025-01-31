## NDMF Platform API

The NDMF platform API allows for integrating the NDMF build pipeline into various VR platforms.
It includes hooks for:

1. Identifying avatar roots
2. Extracting common information about avatars
3. Initiating build processes
4. Controlling which plugins and passes run
5. Reporting platform-specific errors or other information

## Platform provider API

Each platform provider must implement the following interface:

```csharp

interface INDMFPlatformProvider {
    string CanonicalName { get; }
    string DisplayName { get; }
    Texture2D? Icon { get; }
    
    /// If this platform has a specific root component, return it here.
    /// Otherwise, avatars will be probed using `NDMF Avatar Descriptor` components.
    Type? AvatarRootComponentType { get; }
    
    IBuildUI? CreateBuildUI();
    
    bool HasNativeUI { get; }
    void OpenNativeUI();
    
    CommonAvatarInfo ExtractCommonAvatarInfo(GameObject avatarRoot);
    
    /// Return true if we can initialize this platform's native config from this common config structure
    bool CanInitFromCommonAvatarInfo(GameObject avatarRoot, CommonAvatarInfo info);
    /// Initialize this platform's native config from this common config structure (destructive operation)
    void InitFromCommonAvatarInfo(GameObject avatarRoot, CommonAvatarInfo info); 
}
```

To register your plugin, attach the `NDMFPlatformProvider` attribute to your implementing class:

```csharp

[NDMFPlatformProvider]
class MyPlatformProvider : INDMFPlatformProvider {
    // ...
}

```

When selecting a canonical name, please use a dotted-namespace format (e.g. `com.example.platform`).
Unadorned names (e.g. `vrchat`) are reserved for NDMF project use or official support by the platform in question.

## Common information

Certain types of information are generally needed for multiple platforms. For example, eye position,
visemes, etc. This information is encapsulated in the CommonAvatarInfo structure:

```csharp

struct CommonAvatarInfo {
    string? AvatarName;
    string? AvatarDescription;
    string? AvatarAuthor;
    
    Vector2? EyePosition;
    VisemeInfo? Visemes;
    EyeMovementConfiguration? EyeMovement;
    // ...
}
```

More fields may be added in the future.

The purpose of this structure is to allow information to be synced between different platforms; for example,
a user might attach a `NDMF Portable Avatar Descriptor` specifying VRChat as the primary platform. They then build
for resonite; NDMF automatically extracts eyelook position, visemes, etc from the VRChat avatar descriptor, and applies
it to the resonite avatar descriptor.

## Build UI

The build UI is a platform-specific UI that allows the user to initiate the build process. Platforms will implement 
something like:

```csharp

interface IBuildUI {
    GameObject SelectedAvatar { get; set; }
    VisualElement Element { get; }
}

```

This build UI will be displayed in the NDMF UI. It's up to the element in question to trigger the build process, including
calling `AvatarProcessor.ProcessAvatar` as appropriate. You may also display some UI to indicate that it is not currently
possible to build.

The element in question is created when `INDMFPlatformProvider.CreateBuildUI` is called. As such, `Element` can return
`this` if desired.

If you do not support a build UI, you can return `null` from `CreateBuildUI`. In this case, as an alternative, you can
implement

```csharp
    bool HasNativeUI { get; }
    void OpenNativeUI();
```

NDMF will offer a generic button to open the native UI (e.g. VRCSDK window) in this case.

## Native error UI

In some cases, builds might fail for reasons that are outside of your NDMF glue's control. For example, VRCSDK
validation failures. To help make these visible, you can invoke `NDMFPlatformSupport.SetNativeErrorUI(VisualElement)`,
passing arbitrary UI to surface to the user.

## Controlling plugins and passes

Plugins and passes can opt-out of specific platforms, or run only on specific platforms.

You have two ways of doing this; first, by using attributes:

```csharp

[NDMFPlatform(only="vrchat")]
class MyPlugin : Plugin<MyPlugin> {
    // ...
}

```