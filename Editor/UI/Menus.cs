#region

using System;
using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using nadena.dev.ndmf.config;
using nadena.dev.ndmf.cs;
using nadena.dev.ndmf.runtime;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

#endregion

namespace nadena.dev.ndmf.ui
{
    internal static class Menus
    {
        private const string APPLY_ON_PLAY_MENU_NAME = "Tools/NDM Framework/Apply on Play";
        private const string APPLY_ON_BUILD_MENU_NAME = "Tools/NDM Framework/Apply on Build";
        private const string TOPLEVEL_MANUAL_BAKE_MENU_NAME = "Tools/NDM Framework/Manual bake avatar";
        internal const string ENABLE_PREVIEW_MENU_NAME = "Tools/NDM Framework/Enable Previews";
        private const int APPLY_ON_PLAY_PRIO = 1;
        private const int APPLY_ON_BUILD_PRIO = APPLY_ON_PLAY_PRIO + 1;
        private const int TOPLEVEL_MANUAL_BAKE_PRIO = APPLY_ON_BUILD_PRIO + 1;
        internal const int ENABLE_PREVIEW_PRIO = TOPLEVEL_MANUAL_BAKE_PRIO + 1;

        [InitializeOnLoadMethod]
        static void Init()
        {
            EditorApplication.delayCall += OnSettingsChanged;
            Config.OnChange += OnSettingsChanged;
        }

        [MenuItem("GameObject/NDM Framework/Manual bake avatar", true, 49)]
        private static bool ValidateManualBakeGameObject()
        {
            return AvatarProcessor.CanProcessObject(Selection.activeGameObject);
        }

        [MenuItem("GameObject/NDM Framework/Manual bake avatar", false, 49)]
        private static void ManualBakeGameObject()
        {
            AvatarProcessor.ProcessAvatarUI(Selection.activeGameObject);
        }

        [MenuItem(TOPLEVEL_MANUAL_BAKE_MENU_NAME, true, TOPLEVEL_MANUAL_BAKE_PRIO)]
        private static bool ValidateManualBakeToplevel()
        {
            return AvatarProcessor.CanProcessObject(Selection.activeGameObject);
        }

        [MenuItem(TOPLEVEL_MANUAL_BAKE_MENU_NAME, false, TOPLEVEL_MANUAL_BAKE_PRIO)]
        private static void ManualBakeToplevel()
        {
            AvatarProcessor.ProcessAvatarUI(Selection.activeGameObject);
        }

        [MenuItem(APPLY_ON_PLAY_MENU_NAME, false, APPLY_ON_PLAY_PRIO)]
        private static void ApplyOnPlay()
        {
            Config.ApplyOnPlay = !Config.ApplyOnPlay;
        }


        [MenuItem(APPLY_ON_BUILD_MENU_NAME, false, APPLY_ON_BUILD_PRIO)]
        private static void ApplyOnBuild()
        {
            Config.ApplyOnBuild = !Config.ApplyOnBuild;
        }

        private static void OnSettingsChanged()
        {
            Menu.SetChecked(APPLY_ON_PLAY_MENU_NAME, Config.ApplyOnPlay);
            Menu.SetChecked(APPLY_ON_BUILD_MENU_NAME, Config.ApplyOnBuild);
        }
#if NDMF_DEBUG
        [MenuItem("Tools/NDM Framework/Debug Tools/Domain Reload", false, 101)]
        private static void DomainReload()
        {
            EditorUtility.RequestScriptReload();
        }

        [MenuItem("Tools/NDM Framework/Debug Tools/Invalidate shadow hierarchy", false, 101)]
        private static void InvalidateShadowHierarchy()
        {
            nadena.dev.ndmf.cs.ObjectWatcher.Instance.Hierarchy.InvalidateAll();
        }
#endif
        
#if UNITY_2022_1_OR_NEWER
        [MenuItem("Tools/NDM Framework/Debug Tools/Profile build", false, 101)]
        private static void ProfileBuild()
        {
            var av = Selection.activeGameObject;
            IEnumerator coro = ProfileBuildCoro(av);

            EditorApplication.CallbackFunction updateCall = null;
            updateCall = () =>
            {
                if (coro == null)
                {
                    EditorApplication.update -= updateCall;
                }
                else
                {
                    coro.MoveNext();
                }
            };
            
            EditorApplication.update += updateCall;
        }

        private static bool _profilerArmed;

        private static bool ProfilerArmed
        {
            get => _profilerArmed;
            set
            {
                if (value == _profilerArmed) return;
                _profilerArmed = value;
                if (value)
                {
                    ProfilerRecording = true;
                    frameTimer = new Stopwatch();
                    frameTimer.Start();
                    EditorApplication.update += CheckFrameTimeForProfiler;
                }
                else
                {
                    EditorApplication.update -= CheckFrameTimeForProfiler;
                }

                Menu.SetChecked("Tools/NDM Framework/Debug Tools/Arm frametime profiler", value);
            }
        }

        private static Stopwatch frameTimer;

        private static void CheckFrameTimeForProfiler()
        {
            if (frameTimer.ElapsedMilliseconds > 5000)
            {
                EditorApplication.delayCall += () => { ProfilerRecording = false; };
                ProfilerArmed = false;
            }

            frameTimer.Restart();
        }

        [MenuItem("Tools/NDM Framework/Debug Tools/Arm frametime profiler", false, 101)]
        private static void ArmProfiler()
        {
            ProfilerArmed = !ProfilerArmed;
        }

        [MenuItem("Tools/NDM Framework/Debug Tools/Profile build", true, 101)]
        private static bool ProfileBuild_Validate()
        {
            return Selection.activeGameObject != null 
                   && RuntimeUtil.IsAvatarRoot(Selection.activeGameObject.transform);
        }

        private static Func<bool> GetProfilerRecordingState;
        private static Action<bool> SetProfilerRecordingState;

        [InitializeOnLoadMethod]
        private static void InitProfilerAccess()
        {
            GetProfilerRecordingState = () => false;
            SetProfilerRecordingState = _ => { };

            var ty_PW = typeof(ProfilerWindow);

            var m_OnTargetedEditorConnectionChanged =
                ty_PW.GetMethod("OnTargetedEditorConnectionChanged", BindingFlags.Instance | BindingFlags.NonPublic);

            var m_SetRecordingEnabled =
                ty_PW.GetMethod("SetRecordingEnabled", BindingFlags.Instance | BindingFlags.NonPublic);

            var ty_EditorConnectionTarget = m_OnTargetedEditorConnectionChanged?.GetParameters()[0].ParameterType;
            if (ty_EditorConnectionTarget == null) return;

            var MainEditorProcessEditmode = Enum.Parse(ty_EditorConnectionTarget, "MainEditorProcessEditmode");

            GetProfilerRecordingState = () =>
            {
                var profWindow = EditorWindow.GetWindow<ProfilerWindow>();
                return (bool)m_SetRecordingEnabled.Invoke(profWindow, new object[] { null });
            };

            SetProfilerRecordingState = state =>
            {
                var profWindow = EditorWindow.GetWindow<ProfilerWindow>();
                m_OnTargetedEditorConnectionChanged.Invoke(profWindow, new[] { MainEditorProcessEditmode });
                m_SetRecordingEnabled.Invoke(profWindow, new object[] { state });
            };
        }

        private static bool ProfilerRecording
        {
            get => GetProfilerRecordingState();
            set => SetProfilerRecordingState(value);
        }
        
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private static IEnumerator ProfileBuildCoro(GameObject av)
        {
            ProfilerRecording = true;
            
            yield return null; // wait one frame

            if (av == null) yield break;

            var clone = Object.Instantiate(av);

            try
            {
                AvatarProcessor.ProcessAvatar(clone);

            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            //yield return null;
            ProfilerRecording = false;
            yield return null;
            
            Object.DestroyImmediate(clone);
            AvatarProcessor.CleanTemporaryAssets();
        }
#endif
    }
}