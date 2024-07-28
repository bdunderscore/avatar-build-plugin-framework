﻿#region

using System;
using System.Linq;
using System.Threading.Tasks;
using nadena.dev.ndmf.rq;
using nadena.dev.ndmf.rq.unity.editor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

#endregion

namespace nadena.dev.ndmf.preview
{
    internal class ProxyObjectController : IDisposable
    {
        private readonly ProxyObjectCache _cache;
        private readonly Renderer _originalRenderer;
        private Renderer _replacementRenderer;
        internal Renderer Renderer => _replacementRenderer;
        public bool IsValid => _originalRenderer != null && _replacementRenderer != null;

        internal RenderAspects ChangeFlags;

        internal Material[] _initialMaterials;
        internal Mesh _initialSharedMesh;
        internal ComputeContext _monitorRenderer, _monitorMaterials, _monitorMesh;

        internal Task OnInvalidate;

        public static bool IsProxyObject(GameObject obj)
        {
            return ProxyObjectCache.IsProxyObject(obj);
        }
        
        public ProxyObjectController(ProxyObjectCache cache, Renderer originalRenderer, ProxyObjectController _priorController)
        {
            _cache = cache;
            _originalRenderer = originalRenderer;
            
            SetupRendererMonitoring(originalRenderer);
            
            if (_priorController != null)
            {
                if (_priorController._monitorRenderer.OnInvalidate.IsCompleted)
                {
                    ChangeFlags |= RenderAspects.Shapes;
                    
                    if (!_initialMaterials.SequenceEqual(_priorController._initialMaterials))
                    {
                        ChangeFlags |= RenderAspects.Material | RenderAspects.Texture;
                    }
                    
                    if (_initialSharedMesh != _priorController._initialSharedMesh)
                    {
                        ChangeFlags |= RenderAspects.Mesh;
                    }
                }

                if (_priorController._monitorMaterials.OnInvalidate.IsCompleted)
                {
                    ChangeFlags |= RenderAspects.Material | RenderAspects.Texture;
                }
                
                if (_priorController._monitorMesh.OnInvalidate.IsCompleted)
                {
                    ChangeFlags |= RenderAspects.Mesh;
                }

                if (ChangeFlags != 0)
                {
                    Debug.Log("=== ProxyObjectController for " + originalRenderer.gameObject.name + " flags=" +
                              ChangeFlags);
                }
            }

            CreateReplacementObject();
        }

        private void SetupRendererMonitoring(Renderer r)
        {
            _monitorRenderer = new ComputeContext();
            _monitorMaterials = new ComputeContext();
            _monitorMesh = new ComputeContext();

            _monitorRenderer.Observe(r);
            if (r is SkinnedMeshRenderer smr)
            {
                _monitorMesh.Observe(smr.sharedMesh);
                _initialSharedMesh = smr.sharedMesh;
            }
            else if (r is MeshRenderer mr)
            {
                var meshRenderer = _monitorMesh.GetComponent<MeshFilter>(r.gameObject);
                if (meshRenderer != null)
                {
                    _monitorMesh.Observe(meshRenderer.sharedMesh);
                    _initialSharedMesh = meshRenderer.sharedMesh;
                }
            }

            _initialMaterials = (Material[]) r.sharedMaterials.Clone();
            foreach (var material in r.sharedMaterials)
            {
                _monitorMaterials.Observe(material);
                if (material == null) continue;
                
                var texPropIds = material.GetTexturePropertyNameIDs();
                foreach (var texPropId in texPropIds)
                {
                    var tex = material.GetTexture(texPropId);
                    if (tex != null)
                    {
                        _monitorMaterials.Observe(tex);
                    }
                }
            }
            
            OnInvalidate = Task.WhenAny(_monitorRenderer.OnInvalidate, _monitorMaterials.OnInvalidate, _monitorMesh.OnInvalidate);
        }

        internal bool OnPreFrame()
        {
            if (_replacementRenderer == null || _originalRenderer == null)
            {
                return false;
            }

            SkinnedMeshRenderer smr = null;
            if (_originalRenderer is SkinnedMeshRenderer smr_)
            {
                smr = smr_;

                var replacementSMR = (SkinnedMeshRenderer)_replacementRenderer;
                replacementSMR.sharedMesh = smr_.sharedMesh;
                replacementSMR.bones = smr_.bones;
            }
            else
            {
                var originalFilter = _originalRenderer.GetComponent<MeshFilter>();
                var filter = _replacementRenderer.GetComponent<MeshFilter>();
                filter.sharedMesh = originalFilter.sharedMesh;
            }

            _replacementRenderer.sharedMaterials = _originalRenderer.sharedMaterials;

            var target = _replacementRenderer;
            var original = _originalRenderer;

            if (target.gameObject.scene != original.gameObject.scene &&
                original.gameObject.scene.IsValid())
            {
                SceneManager.MoveGameObjectToScene(target.gameObject, original.gameObject.scene);
            }

            target.transform.position = original.transform.position;
            target.transform.rotation = original.transform.rotation;

            target.localBounds = original.localBounds;
            if (target is SkinnedMeshRenderer targetSMR && original is SkinnedMeshRenderer originalSMR)
            {
                targetSMR.rootBone = originalSMR.rootBone != null ? originalSMR.rootBone : originalSMR.transform;
                targetSMR.quality = originalSMR.quality;

                if (targetSMR.sharedMesh != null)
                {
                    var blendShapeCount = targetSMR.sharedMesh.blendShapeCount;
                    for (var i = 0; i < blendShapeCount; i++)
                    {
                        targetSMR.SetBlendShapeWeight(i, originalSMR.GetBlendShapeWeight(i));
                    }
                }
            }

            target.shadowCastingMode = original.shadowCastingMode;
            target.receiveShadows = original.receiveShadows;
            target.lightProbeUsage = original.lightProbeUsage;
            target.reflectionProbeUsage = original.reflectionProbeUsage;
            target.probeAnchor = original.probeAnchor;
            target.motionVectorGenerationMode = original.motionVectorGenerationMode;
            target.allowOcclusionWhenDynamic = original.allowOcclusionWhenDynamic;

            return true;
        }

        private void CreateReplacementObject()
        {
            if (_originalRenderer == null) return;

            _replacementRenderer = _cache.GetOrCreate(_originalRenderer, () =>
            {
                var replacementGameObject = new GameObject("Proxy renderer for " + _originalRenderer.gameObject.name);
                replacementGameObject.hideFlags = HideFlags.DontSave;

#if MODULAR_AVATAR_DEBUG_HIDDEN
                replacementGameObject.hideFlags = HideFlags.DontSave;
#endif

                replacementGameObject.AddComponent<SelfDestructComponent>().KeepAlive = this;

                Renderer renderer;
                if (_originalRenderer is SkinnedMeshRenderer smr)
                {
                    renderer = replacementGameObject.AddComponent<SkinnedMeshRenderer>();
                }
                else if (_originalRenderer is MeshRenderer mr)
                {
                    renderer = replacementGameObject.AddComponent<MeshRenderer>();
                    replacementGameObject.AddComponent<MeshFilter>();
                }
                else
                {
                    Debug.Log("Unsupported renderer type: " + _originalRenderer.GetType());
                    Object.DestroyImmediate(replacementGameObject);
                    return null;
                }

                return renderer;
            });
        }

        public void Dispose()
        {
            if (_replacementRenderer != null)
            {
                _cache.ReturnProxy(_originalRenderer, _replacementRenderer);
                _replacementRenderer = null;
            }
        }
    }
}