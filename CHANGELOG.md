# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- [#467] Added the `AnimatorServicesContext` and lots of supporting APIs for working with animator controllers.

### Fixed

### Changed

### Removed

### Security

### Deprecated

## [1.6.7] - [2025-01-26]

### Fixed
- Fixed another source of NullReferenceErrors after deleting game objects (#522)

## [1.6.6] - [2025-01-25]

### Fixed
- NullReferenceErrors when there are missing components, breaking scene view movement (#520)

## [1.6.5] - [2025-01-24]

### Fixed
- Performance improvements (#515 #516)
- "Test build" button is disabled after test-building an avatar that is not a scene root (#518)

## [1.6.4] - [2025-01-18]

### Fixed
- [#507] Fixed a potential editor freeze bug

### Removed
- [#507] Removed the `NDMFSyncContext.RunOnMainThread` API which was accidentally addded in 1.6.3.
  This might be re-added in 1.7.0.

## [1.6.3] - [2025-01-15]

### Fixed
- [#500] Fixed a thread-safety issue which could cause various issues, including editor performance degradation.
- [#506] Fixed an issue with the preview preferences dialog

## [1.6.2] - [2024-12-04]

### Added
- [#486] Add Simplified Chinese support
### Fixed
- [#487] Fixed a performance issue where all assets would potentially be loaded on reimport, taking a lot of time and
  memory in the process

## [1.6.1] - [2024-11-28]

**This release does not confirm to semver**. Due to unexpected compatibility issues with `IExtensionContext.Owner`, this
release removes this API. Although this is technically a semver violation, it's better than the accidental violation in
1.6.0.

**Note**: In 1.7.0 NDMF will add sentinel default methods to all interfaces to ensure that downstream packages are not
using outdated versions of C# to build. The NDMF project will not consider this to be a breaking change after 1.7.0.
Please update your projects to build with modern C# before then.

### Fixed
- [#482] Additional performance improvements

### Removed
- [#483] Removed `IExtensionContext.Owner` due to compatibility issues.

## [1.6.0] - [2024-11-28]

### Added
- [#479] Added `IAssetSaver` and `SerializationScope` APIs.
  - NDMF plugins are encouraged to use `IAssetSaver.SaveAsset` instead of directly accessing `AssetContainer`. This will
    split saved assets across multiple files, to avoid performance degradation as the number of assets in a container
    grows.
- [#480] Added `IExtensionContext.Owner` API. Setting this property will allow errors to be correctly attributed to the
  plugin that contains an extension context.
- [#472] [#474] Added the `DependsOnContext` attribute, for declaring dependencies between extension contexts.
- [#473] Added `BuildContext.SetEnableUVDistributionRecalculation` to allow opting out from the automatic call to
  `Mesh.RecalculateUVDistributionMetrics` on generated meshes.
- [#478] Added `ProfilerScope` API
- [#481] Added `NDMFPreview.GetOriginalObjectForProxy` API

### Fixed
- [#479] Unpacking generated asset containers can break inter-asset references

## [1.5.7] - [2024-11-17]

### Changed
- [#468] Performance improvements
- [#471] Improve compatibility with VRChat's streaming mipmaps feature
  - NDMF will now call `Mesh.RecalculateUVDistributionMetrics` on all generated meshes on upload. An API to opt-out will
    be provided at the next major version release.

## [1.5.6] - [2024-10-19]

### Fixed

- [#461] Selection outlines in scene view do not work reliably when preview system is active
- [#459] "Proxy object was destroyed improperly! Resetting pipeline..." error appears frequently
- [#460] Preview system fails to recover when the primary proxy is destroyed
- [#462] NDMF console fails to appear when an ExtensionContext throws an exception

## [1.5.5] - [2024-10-15]

### Fixed

- [#454] Scene view selection outlines flicker in some cases
- [#450] Improved performance when a large number of object change events are generated (e.g. when exiting animation
  mode)
- [#441] Fixed an issue where the preview object pickable status could get out of sync with the original
- [#444] Fixed an issue where the preview system broke drag-and-drop of materials onto the scene view
- [#444] Fixed an issue where the preview system broke drag-to-select in the scene view

### Changed

- [#451] Changed preview system to apply to more cameras. Specifically, we now handle all cameras with no render
  texture set, to ensure that Game Mode behaves as expected.

## [1.5.4] - [2024-10-05]

### Added

- [#439] Added a menu option to disable NDMF processing on build (`Tools -> NDM Framework -> Apply on Build`).
  - This setting will survive domain reloads but not editor restarts.

### Fixed

- [#438] Fixed an issue where exceptions would be thrown when scenes are unloaded

## [1.5.3] - [2024-10-03]

### Fixed
- [#435] Fixed an issue where `File -> Save As` could break due to the internal preview scene becoming selected

## [1.5.2] - [2024-10-02]

### Fixed
- [#434] Pass Harmony ID to `Harmony.UnpatchAll()` to avoid double-unpatching methods
- [#433] Remove stray debug print

## [1.5.1] - [2024-09-30]

### Fixed
- [#431] Compile error when test framework is not present

## [1.5.0] - [2024-09-29]

### Removed
- Unity 2019 is no longer supported.

### Added
- [#244] Added a framework that can be used to override the rendering of an object without modifying the object itself
- [#297] Added UI for turning preview on/off at a plugin or pass level
- [#244] Added a framework for observing scene object changes and reacting to them.
- [#244] Added `SelfDestructComponent` (useful for hidden preview-only components)
- [#312] Added a default value field to ProvidedParameter
- [#312] Added support for invalidating ComputeContext to ParameterInfo
- [#360] Added `AsyncProfiler` to help profile code running in Tasks
- [#365] Added debug tool to profile long editor frames
- [#407] Added `ProvidedParameter.ExpandTypeOnConflict` to resolve parameter type mismatch automatically
- [#410] Added `NDMFSyncContext` API
- [#424] Added tracing system for the preview/invalidation system

### Fixed
- [#260] [ChilloutVR] Fix: Build fails due to CVRAvatar preventing recreation of Animator (contributed by @hai-vr)
- [#261] [ChilloutVR] feat: don't build the avatar when ChilloutVR shows the upload UI (contributed by @hai-vr)
- [#280] Console warnings issued whenever `.unity` (scene) files are saved
- [#341] Font rendering breaks on scene change
- [#385] Fix: parameter introspection used default value from child, not parent
- [#386]
  Workaround [VRCSDK bug](https://feedback.vrchat.com/sdk-bug-reports/p/string-conversion-errors-from-runtimeassemblygetcodebase-with-japanese-locale-an)
  caused by non-ASCII project paths.
- [#399] Fix: Parameter introspection did not skip EditorOnly objects
- [#416] Fixed issues where assets would not properly be tracked due to C# object recreation edge cases
  (removed ObjectIdentityComparer)

### Changed
- [#266] NDMF language defaults to being based on the system language.
- [#408] Unserialized assets will be serialized after the Transforming phase completes (before e.g. VRCFury runs)

## [1.4.1] - [2024-05-12]

### Added
- Manual Bake avatar context menu for projects with Modular Avatar (#234)
- `__Generated` folder is not removed after building avatar (#235)
- Add Traditional Chinese support (#230)

### Fixed
- Workaround VRCSDK bug where stale PhysBones state could be retained over play mode transitions (#231) 
- Show object name when we're unable to find the actual GameObject, in error display UI (#224)
- Rerender error report window when leaving play mode (#237)
- Deduplicate object references in NDMF console error reports (#237)

### Changed
- Renamed `NDMF Error Report` to `NDMF Console` (#222)
- Changed Japanese strings a bit (#222)

### Removed

### Security

### Deprecated

## [1.4.0] - [2024-03-27]

### Added
- Added a new API to allow NDMF plugins to declare and introspect expressions parameter usage (#184)
- Added an API to select a non-broken font for use in UI Elements based on the current locale (#190)
- Added `[NDMFInternal]` attribute (#217)
- Added a debug feature to profile a test build (#214)
- Added `ParameterProvider.SubParameters()` for `ParameterNamespace.PhysBonesPrefix` parameters (#196)
- Added support for declaring ProvidesParametersFor via base classes and interfaces (#198)

### Fixed
- Specify zh-* font to make the font normal (#206)
- Hide certain subassets after manual bake and/or extracting assets (#212)
- Fixed issues with capitalization in language preference (#215)
- Apply on play isn't suppressed when Av3mu is present (#200)
- UIElementLocalizer could fail to find localized strings in some cases (#189)

### Changed
- In ParameterProvider, the parameter type of PhysBone Contact Receiver is now the type corresponding to the receiver type. (#209)

## [1.3.7] - [2024-03-06]

### Fixed
- Fix language code not recognizable due to inconsistent case

## [1.3.6] - [2024-02-27]

### Fixed
- Having multiple language variants that differ in case (e.g. `en-us` vs `en-US`) would break the language switcher (#176)

## [1.3.5] - [2024-02-26]

### Fixed
- Incorrect language switcher behavior when locales were filtered from display (#175)

## [1.3.4] - [2024-02-18]

### Fixed
- Incorrect language display names for some locales (#171)

### Changed
- Uncultured language variants (e.g. `en`) are not displayed in the language switcher when cultured variants (e.g.
  `en-US`) are registered. (#171)

## [1.3.3] - [2024-02-12]

### Fixed
- VRCSDK builds fail due to UnityEditor-only type references (#167)

## [1.3.2] - [2024-02-12]

### Fixed
- Avatar names with leading/trailing whitespace broke builds (#161) 
- Ave3mu's "Run Preprocess Avatar Hook" option was force-enabled even when apply on play was disabled (#160)
- Animator cloning broke "Among Us" follower due to sus processing order (#165)

### Changed
- Changed the hook processing logic to closer match VRCSDK (improves compatibility with VRCF and other external hooks)
  (#162)

### Removed

### Security

### Deprecated

## [1.3.1] - [2024-02-06]

### Fixed
- Apply on Play did not work for non-VRChat avatars or environments (#153)
- Error if some scene is not loaded (#156)
- Outfit sub-animators can cause transforms to move when using GestureManager (#147)

### Changed
- Run all preprocess hooks in Apply On Play processing, to better align with VRCFury handling. (#145)

## [1.3.0] - [2024-01-29]

### Added
- New localization framework
- New error reporting framework
- API to record when one object is replaced by another
- Added a non-component-based check for double execution of hooks (#142)
- Exposed the qualified name variant of `Sequence.BeforePass` (#109)

### Changed
- Adjusted hook processing order to improve compatibility with VRCFury (#122)
- Worked around a hack in VRCFury that broke optimization plugins (#126)

### Fixed
- Play mode processing fails when installed via UPM (#89)

## [1.2.5] - [2023-11-15]

### Fixed
- GUID collisions with packages derived from the VRChat template package (#84)

## [1.2.4] - [2023-11-12]

### Fixed
- Duplicate references error when Lyuma's Av3emulator is installed due to Unity bug (#80)

## [1.2.3] - [2023-11-11]

### Added

### Fixed
- Fixed an issue where apply on play might not work when multiple scenes are open (#61)
- Fixed an issue where Apply on Play would not work properly when Lyuma's Av3Emulator had preprocess hooks disabled
  (bdunderscore/modular-avatar#516) (#64)

### Changed

- Make Apply on Play non-persistent, as users seem to frequently have issues with it left turned off.

### Removed
- Removed a vestigial "Avatar Toolkit -> Apply on Play" menu item, which didn't do anything when selected. (#70)

### Security

### Deprecated
- Deprecated APIs for finding avatar roots defined outside RuntimeUtil (#73)

## [1.2.2] - [2023-10-09]

### Fixed
- Fixed an issue where apply on play might not work when multiple scenes are open (#61)

## [1.2.1] - [2023-10-08]

### Fixed
- Removed .git folder from built packages

## [1.2.0] - [2023-10-07]

### Fixed
- Re-released 1.1.1 as 1.2.0 as it contained new APIs (#34). Please use 1.2.0 for version declarations if these new APIs
  are needed.

## [1.1.1] - [2023-10-07]

### Added
- Exposed APIs for finding avatar roots in RuntimeUtil (#34)
- Support Unity projects without VRCSDK (#50)
- Add warning when an outdated version of Modular Avatar is installed (#54)

## [1.1.0] - [2023-10-05]

### Added
- Added toplevel menu for manual bake avatar, even when MA is also installed (#35)
- Added support for multiple ExportsPlugin declarations (#40)
- Added additional convenience overloads for BeforePlugin and AfterPlugin

### Fixed
- Create ApplyOnPlayGlobalActivator correctly when creating and opening scenes (#31)
- Time resolution on the plugin sequence display is milliseconds, not 0.01 ms (#43)
- BeforePlugin/AfterPlugin declarations don't work (#44)

### Changed
- Position of the Plugin sequence display window is now preserved after restarting the Unity Editor (#42)

### Removed

### Security

## [1.0.2] - [2023-09-26]

### Added
* Added missing license declaration

### Fixed
- Generated assets are not visible on the Project window (#26)
- Apply on play is not disabled if av3emulator is active on other scene
- Avatar names with non-path-safe characters break builds (again) (#18)

### Changed

### Removed

### Security

## [1.0.1] - [2023-09-24]

### Fixed

- Move optimization passes to -1025 to ensure it runs before VRChat destroys IEditorOnly components.

## [1.0.0] - [2023-09-24]

### Added

- Initial public release

### Fixed

- Remove missing script components from the avatar at the start of the build.
- Fixed build failures when avatar names contained non-path-safe characters (#18)
- Suppress apply-on-play when Lyuma's Av3Emulator is active (improves compatibility) (#16)

### Changed

### Removed

- Samples have been removed from the build, as they are not part of the stable NDMF API surface area. They'll probably
  be moved to a separate repository at some point.

## [0.4.0] - [2023-09-20]

0.4.0 and prior releases did not Keep A Changelog.
