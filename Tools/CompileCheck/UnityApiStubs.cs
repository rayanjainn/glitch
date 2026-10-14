// Minimal, hand-written signatures of the Unity APIs this project uses.
// PURPOSE: type-check project code outside Unity (cross-references between our own
// classes, missing usings, wrong argument types). These are NOT Unity's real
// assemblies: a member that exists here may not exist in Unity, so a clean build
// is weaker evidence than a Unity compile. Bodies are never executed.
#pragma warning disable CS0067, CS0626, CS0660, CS0661, CS8618, CS0414
using System;
using System.Collections.Generic;

namespace UnityEngine
{
    public class Object
    {
        public string name { get => throw null; set { } }
        public static bool operator ==(Object a, Object b) => throw null;
        public static bool operator !=(Object a, Object b) => throw null;
        public static implicit operator bool(Object o) => throw null;
        public static void Destroy(Object o) { }
        public static void DestroyImmediate(Object o) { }
        public static T Instantiate<T>(T original) where T : Object => throw null;
        public static T FindAnyObjectByType<T>() where T : Object => throw null;
        public static T Instantiate<T>(T original, Vector3 position, Quaternion rotation, Transform parent) where T : Object => throw null;
        public int GetInstanceID() => throw null;
    }
    public class YieldInstruction { }
    public class AsyncOperation : YieldInstruction { public bool isDone => throw null; }
    public sealed class WaitForFixedUpdate : YieldInstruction { }
    public class ScriptableObject : Object { public static T CreateInstance<T>() where T : ScriptableObject => throw null; }
    public class Sprite : Object { }
    [AttributeUsage(AttributeTargets.Class)] public class CreateAssetMenuAttribute : Attribute { public string menuName; public string fileName; public int order; }
    public static class ColorUtility { public static string ToHtmlStringRGB(Color c) => throw null; }

    // Unity 6.x engine type. Present so a project type with the same simple name
    // produces the same CS0104 ambiguity here that it does in Unity.
    public struct EntityId { }
    public class Component : Object
    {
        public GameObject gameObject => throw null;
        public Transform transform => throw null;
        public string tag { get => throw null; set { } }
        public T GetComponent<T>() => throw null;
        public T GetComponentInParent<T>() => throw null;
        public T GetComponentInParent<T>(bool includeInactive) => throw null;
        public T[] GetComponents<T>() => throw null;
        public T[] GetComponentsInChildren<T>(bool includeInactive) => throw null;
        public void GetComponentsInChildren<T>(bool includeInactive, List<T> results) { }
        public T GetComponentInChildren<T>() => throw null;
        public T GetComponentInChildren<T>(bool includeInactive) => throw null;
        public bool CompareTag(string t) => throw null;
    }
    public class Behaviour : Component { public bool enabled { get => throw null; set { } } public bool isActiveAndEnabled => throw null; }
    public class MonoBehaviour : Behaviour { }
    public class GameObject : Object
    {
        public GameObject() { }
        public GameObject(string name) { }
        public GameObject(string name, params Type[] components) { }
        public Transform transform => throw null;
        public int layer { get => throw null; set { } }
        public T GetComponentInChildren<T>(bool includeInactive) => throw null;
        public bool isStatic { get => throw null; set { } }
        public string tag { get => throw null; set { } }
        public bool activeInHierarchy => throw null;
        public bool activeSelf => throw null;
        public SceneManagement.Scene scene => throw null;
        public void SetActive(bool v) { }
        public T AddComponent<T>() where T : Component => throw null;
        public T GetComponent<T>() => throw null;
        public T[] GetComponentsInChildren<T>(bool includeInactive) => throw null;
        public T[] GetComponentsInChildren<T>() => throw null;
        public T[] GetComponents<T>() => throw null;
        public static GameObject CreatePrimitive(PrimitiveType t) => throw null;
        public bool CompareTag(string t) => throw null;
    }
    public enum PrimitiveType { Sphere, Capsule, Cylinder, Cube, Plane, Quad }
    public class Transform : Component, System.Collections.IEnumerable
    {
        public Vector3 InverseTransformPoint(Vector3 position) => throw null;
        public void LookAt(Vector3 worldPosition) { }
        public Vector3 position { get => throw null; set { } }
        public Vector3 localPosition { get => throw null; set { } }
        public Quaternion rotation { get => throw null; set { } }
        public Quaternion localRotation { get => throw null; set { } }
        public Vector3 localScale { get => throw null; set { } }
        public Vector3 lossyScale => throw null;
        public Transform parent { get => throw null; set { } }
        public Vector3 forward => throw null; public Vector3 right => throw null; public Vector3 up => throw null;
        public Vector3 TransformPoint(Vector3 p) => throw null;
        public void SetParent(Transform p, bool worldPositionStays) { }
        public void SetPositionAndRotation(Vector3 p, Quaternion r) { }
        public bool IsChildOf(Transform t) => throw null;
        public Transform Find(string n) => throw null;
        public System.Collections.IEnumerator GetEnumerator() => throw null;
    }
    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 zero => default; public static Vector2 up => default;
        public static Vector2 ClampMagnitude(Vector2 v, float m) => throw null;
        public static Vector2 operator +(Vector2 a, Vector2 b) => throw null;
        public static Vector2 operator *(Vector2 a, float b) => throw null;
        public static bool operator ==(Vector2 a, Vector2 b) => throw null;
        public static bool operator !=(Vector2 a, Vector2 b) => throw null;
        public override bool Equals(object o) => throw null; public override int GetHashCode() => throw null;
    }
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 zero => default; public static Vector3 one => default; public static Vector3 up => default;
        public static Vector3 forward => default; public static Vector3 back => default;
        public static Vector3 right => default; public static Vector3 left => default; public static Vector3 down => default;
        public static Vector3 ClampMagnitude(Vector3 v, float m) => throw null;
        public static Vector3 Slerp(Vector3 a, Vector3 b, float t) => throw null;
        public static Vector3 Lerp(Vector3 a, Vector3 b, float t) => throw null;
        public float magnitude => throw null; public float sqrMagnitude => throw null; public Vector3 normalized => throw null;
        public static Vector3 Cross(Vector3 a, Vector3 b) => throw null;
        public static Vector3 Scale(Vector3 a, Vector3 b) => throw null;
        public static float Dot(Vector3 a, Vector3 b) => throw null;
        public static float Distance(Vector3 a, Vector3 b) => throw null;
        public static Vector3 MoveTowards(Vector3 a, Vector3 b, float d) => throw null;
        public static Vector3 SmoothDamp(Vector3 c, Vector3 t, ref Vector3 v, float s) => throw null;
        public static Vector3 operator +(Vector3 a, Vector3 b) => throw null;
        public static Vector3 operator -(Vector3 a, Vector3 b) => throw null;
        public static Vector3 operator *(Vector3 a, float b) => throw null;
        public static Vector3 operator *(float b, Vector3 a) => throw null;
        public static Vector3 operator /(Vector3 a, float b) => throw null;
        public static bool operator ==(Vector3 a, Vector3 b) => throw null;
        public static bool operator !=(Vector3 a, Vector3 b) => throw null;
        public static implicit operator Vector2(Vector3 v) => throw null;
        public static implicit operator Vector3(Vector2 v) => throw null;
        public override bool Equals(object o) => throw null; public override int GetHashCode() => throw null;
    }
    public struct Quaternion
    {
        public static Quaternion identity => default;
        public static Quaternion Euler(float x, float y, float z) => throw null;
        public static Quaternion AngleAxis(float a, Vector3 axis) => throw null;
        public static Quaternion LookRotation(Vector3 f, Vector3 up) => throw null;
        public static Quaternion LookRotation(Vector3 f) => throw null;
        public static Quaternion Euler(Vector3 e) => throw null;
        public static Quaternion RotateTowards(Quaternion a, Quaternion b, float d) => throw null;
        public static Vector3 operator *(Quaternion q, Vector3 v) => throw null;
        public static Quaternion operator *(Quaternion a, Quaternion b) => throw null;
    }
    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b) { this.r = r; this.g = g; this.b = b; a = 1; }
        public Color(float r, float g, float b, float a) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static Color white => default; public static Color blue => default; public static Color magenta => default; public static Color black => default;
        public static Color operator *(Color c, float f) => throw null;
        public static Color Lerp(Color a, Color b, float t) => throw null;
        public static void RGBToHSV(Color rgb, out float h, out float s, out float v) => throw null;
    }
    public struct Color32
    {
        public byte r, g, b, a;
        public Color32(byte r, byte g, byte b, byte a) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static implicit operator Color32(Color c) => throw null;
        public static implicit operator Color(Color32 c) => throw null;
    }
    public struct Vector2Int { public Vector2Int(int x, int y) { } public int x => throw null; public int y => throw null; }
    public struct Pose { public Vector3 position; public Quaternion rotation; public Pose(Vector3 p, Quaternion r) { position = p; rotation = r; } }
    public struct Rect { public Rect(float x, float y, float w, float h) { this.x = x; this.y = y; width = w; height = h; } public float x, y, width, height; }
    public static class PlayerPrefs { public static int GetInt(string k, int d) => throw null; public static void SetInt(string k, int v) { } public static void Save() { } }
    public struct Bounds
    {
        public Bounds(Vector3 c, Vector3 s) { }
        public Vector3 center => throw null; public Vector3 size => throw null; public Vector3 extents => throw null; public Vector3 min => throw null; public Vector3 max => throw null;
        public void Encapsulate(Bounds b) { } public void Encapsulate(Vector3 point) { } public void Expand(float a) { } public bool Contains(Vector3 p) => throw null;
    }
    public struct Ray { }
    public struct RaycastHit { public float distance => throw null; public Transform transform => throw null; public Collider collider => throw null; public Vector3 point => throw null; }
    public struct LayerMask
    {
        public int value { get => throw null; set { } }
        public static int NameToLayer(string n) => throw null;
        public static string LayerToName(int l) => throw null;
    }
    public enum QueryTriggerInteraction { UseGlobal, Ignore, Collide }
    [Flags] public enum CollisionFlags { None = 0, Sides = 1, Above = 2, Below = 4 }
    public static class Physics
    {
        public const int AllLayers = -1;
        public static void SyncTransforms() { }
        public static bool Raycast(Vector3 origin, Vector3 direction, float maxDistance, int layerMask, QueryTriggerInteraction q) => throw null;
        public static bool SphereCast(Vector3 o, float r, Vector3 d, out RaycastHit h, float max, int mask, QueryTriggerInteraction q) => throw null;
        public static int SphereCastNonAlloc(Ray ray, float r, RaycastHit[] results, float max, int mask, QueryTriggerInteraction q) => throw null;
        public static void IgnoreLayerCollision(int a, int b, bool ignore) { }
        public static bool CapsuleCast(Vector3 p1, Vector3 p2, float radius, Vector3 direction, float maxDistance, int mask, QueryTriggerInteraction q) => throw null;
        public static bool CheckBox(Vector3 center, Vector3 halfExtents, Quaternion orientation, int mask, QueryTriggerInteraction q) => throw null;
        public static int OverlapBoxNonAlloc(Vector3 center, Vector3 halfExtents, Collider[] results, Quaternion orientation, int mask, QueryTriggerInteraction q) => throw null;
        public static bool GetIgnoreLayerCollision(int a, int b) => throw null;
    }
    public static class Mathf
    {
        public const float PI = 3.14159265f;
        public static float Max(float a, float b) => throw null; public static int Max(int a, int b) => throw null;
        public static float Clamp(float v, float a, float b) => throw null;
        public static int Clamp(int v, int a, int b) => throw null;
        public static float Min(float a, float b) => throw null;
        public static float Min(params float[] values) => throw null;
        public static float InverseLerp(float a, float b, float value) => throw null;
        public static float Clamp01(float v) => throw null;
        public static float Sqrt(float v) => throw null;
        public static float Exp(float v) => throw null;
        public static bool Approximately(float a, float b) => throw null;
        public static float Abs(float v) => throw null; public static float Sin(float v) => throw null; public static float Cos(float v) => throw null;
        public static float Repeat(float t, float l) => throw null; public static float MoveTowards(float a, float b, float d) => throw null;
        public static float Lerp(float a, float b, float t) => throw null;
        public static int RoundToInt(float v) => throw null; public static float Round(float v) => throw null; public static int CeilToInt(float v) => throw null;
    }
    public static class Time { public static float fixedDeltaTime => throw null; public static float deltaTime => throw null; public static float unscaledDeltaTime => throw null; public static float time => throw null; public static float unscaledTime => throw null; public static float timeScale { get => throw null; set { } } }
    public enum CursorLockMode { None, Locked, Confined }
    public static class Cursor { public static CursorLockMode lockState { get => throw null; set { } } public static bool visible { get => throw null; set { } } }
    public static class Screen { public static int width => throw null; public static int height => throw null; public static bool fullScreen { get => throw null; set { } } }
    public static class Application
    {
        public static string unityVersion => throw null; public static bool isPlaying => throw null;
        public static string persistentDataPath => throw null; public static bool isEditor => throw null; public static string temporaryCachePath => throw null;
        public static void Quit() { }
    }
    public static class JsonUtility { public static string ToJson(object obj, bool prettyPrint) => throw null; public static T FromJson<T>(string json) => throw null; }
    public static class Gizmos
    {
        public static Color color { get => throw null; set { } }
        public static void DrawWireSphere(Vector3 c, float r) { }
        public static void DrawLine(Vector3 a, Vector3 b) { }
    }
    public enum LogType { Error = 0, Assert = 1, Warning = 2, Log = 3, Exception = 4 }
    public static class Debug
    {
        public static void Log(object m) { } public static void Log(object m, Object c) { }
        public static void LogWarning(object m) { } public static void LogWarning(object m, Object c) { }
        public static void LogError(object m) { } public static void LogError(object m, Object c) { }
        public static void LogException(Exception e) { }
        public static void LogException(Exception e, Object c) { }
    }
    public class Collider : Component { public bool enabled { get => throw null; set { } } public Bounds bounds => throw null; public Rigidbody attachedRigidbody => throw null; public bool isTrigger { get => throw null; set { } } }
    public class BoxCollider : Collider { public Vector3 center { get => throw null; set { } } public Vector3 size { get => throw null; set { } } }
    public class MeshCollider : Collider { }
    public class CharacterController : Collider
    {
        public float height { get => throw null; set { } } public float radius { get => throw null; set { } }
        public Vector3 center { get => throw null; set { } } public float stepOffset { get => throw null; set { } }
        public float skinWidth { get => throw null; set { } } public float slopeLimit { get => throw null; set { } }
        public bool isGrounded => throw null; public Vector3 velocity => throw null;
        public CollisionFlags Move(Vector3 motion) => throw null;
    }
    public enum ForceMode { Force = 0, Acceleration = 5, Impulse = 1, VelocityChange = 2 }
    public enum RigidbodyInterpolation { None, Interpolate, Extrapolate }
    public enum CollisionDetectionMode { Discrete, Continuous, ContinuousDynamic, ContinuousSpeculative }
    public class Rigidbody : Component { public Vector3 linearVelocity { get => throw null; set { } } public Vector3 angularVelocity { get => throw null; set { } } public void WakeUp() { } public void MovePosition(Vector3 p) { } public bool useGravity { get => throw null; set { } } public Vector3 position { get => throw null; set { } } public void AddForce(Vector3 f, ForceMode m) { } public bool isKinematic { get => throw null; set { } } public float mass { get => throw null; set { } } public RigidbodyInterpolation interpolation { get => throw null; set { } } public CollisionDetectionMode collisionDetectionMode { get => throw null; set { } } }
    public class Renderer : Component
    {
        public bool enabled { get => throw null; set { } } public Bounds bounds => throw null;
        public Material sharedMaterial { get => throw null; set { } }
        public Material[] sharedMaterials { get => throw null; set { } }
        public Rendering.ShadowCastingMode shadowCastingMode { get => throw null; set { } }
        public bool receiveShadows { get => throw null; set { } } public bool allowOcclusionWhenDynamic { get => throw null; set { } }
    }
    public class MeshRenderer : Renderer { }
    public class TrailRenderer : Renderer
    {
        public float time { get => throw null; set { } } public float startWidth { get => throw null; set { } } public float endWidth { get => throw null; set { } }
        public Color startColor { get => throw null; set { } } public Color endColor { get => throw null; set { } }
        public float minVertexDistance { get => throw null; set { } }
    }
    public class ParticleSystemRenderer : Renderer { }
    public class LineRenderer : Renderer
    {
        public bool useWorldSpace { get => throw null; set { } } public bool loop { get => throw null; set { } }
        public int positionCount { get => throw null; set { } }
        public float startWidth { get => throw null; set { } } public float endWidth { get => throw null; set { } }
        public int numCornerVertices { get => throw null; set { } } public int numCapVertices { get => throw null; set { } }
        public Color startColor { get => throw null; set { } } public Color endColor { get => throw null; set { } }
        public void SetPositions(Vector3[] p) { }
        public void SetPosition(int i, Vector3 p) { }
    }
    public class Camera : Behaviour
    {
        public float nearClipPlane { get => throw null; set { } } public float farClipPlane { get => throw null; set { } }
        public float fieldOfView { get => throw null; set { } } public CameraClearFlags clearFlags { get => throw null; set { } }
        public Color backgroundColor { get => throw null; set { } }
        public Ray ViewportPointToRay(Vector3 p) => throw null;
        public Vector3 WorldToScreenPoint(Vector3 p) => throw null;
        public static Camera main => throw null;
    }
    public enum CameraClearFlags { Skybox = 1, SolidColor = 2, Depth = 3, Nothing = 4 }
    public class AudioListener : Behaviour { public static float volume { get => throw null; set { } } }
    public class Font : Object { }
    public sealed class AudioClip : Object
    {
        public static AudioClip Create(string name, int lengthSamples, int channels, int frequency, bool stream) => throw null;
        public bool SetData(float[] data, int offsetSamples) => throw null;
        public bool GetData(float[] data, int offsetSamples) => throw null;
    }
    public sealed class AudioSource : Behaviour
    {
        public bool playOnAwake { get => throw null; set { } }
        public float spatialBlend { get => throw null; set { } }
        public void PlayOneShot(AudioClip clip, float volumeScale) { }
        public bool loop { get => throw null; set { } } public bool isPlaying => throw null;
        public float minDistance { get => throw null; set { } } public float maxDistance { get => throw null; set { } }
        public AudioRolloffMode rolloffMode { get => throw null; set { } }
        public AudioClip clip { get => throw null; set { } } public float volume { get => throw null; set { } }
        public void Play() { } public void Stop() { }
    }
    public enum LightType { Spot, Directional, Point }
    public enum LightShadows { None, Hard, Soft }
    public class Light : Behaviour
    {
        public LightType type { get => throw null; set { } } public Color color { get => throw null; set { } }
        public float intensity { get => throw null; set { } } public float range { get => throw null; set { } }
        public LightShadows shadows { get => throw null; set { } }
    }
    public class Shader : Object { public static Shader Find(string n) => throw null; }
    [Flags] public enum MaterialGlobalIlluminationFlags { None = 0, RealtimeEmissive = 1, BakedEmissive = 2, EmissiveIsBlack = 4 }
    public class Material : Object
    {
        public Material(Shader s) { }
        public Shader shader { get => throw null; set { } }
        public string[] shaderKeywords { get => throw null; set { } }
        public MaterialGlobalIlluminationFlags globalIlluminationFlags { get => throw null; set { } }
        public void SetColor(string n, Color c) { } public void SetFloat(string n, float f) { }
        public void SetTexture(string n, Texture t) { } public void SetTextureScale(string n, Vector2 s) { }
        public void EnableKeyword(string k) { } public void CopyPropertiesFromMaterial(Material m) { }
        public int renderQueue { get => throw null; set { } }
        public void SetOverrideTag(string tag, string value) { }
        public string GetTag(string tag, bool searchFallbacks) => throw null;
    }
    public static class QualitySettings
    {
        public static string[] names => throw null;
        public static int GetQualityLevel() => throw null;
        public static Rendering.RenderPipelineAsset renderPipeline { get => throw null; set { } }
    }
    public enum TextureFormat { RGBA32 = 4 }
    public enum TextureWrapMode { Repeat, Clamp }
    public class Texture : Object { public TextureWrapMode wrapMode { get => throw null; set { } } }
    public class Texture2D : Texture
    {
        public Texture2D(int w, int h, TextureFormat f, bool mipChain) { }
        public static Texture2D whiteTexture => throw null;
        public void SetPixels(Color[] c) { } public void Apply() { }
    }
    public static class ImageConversion { public static byte[] EncodeToPNG(this Texture2D t) => throw null; }
    public sealed class Mesh : Object
    {
        public Vector3[] vertices { get => throw null; set { } } public int[] triangles { get => throw null; set { } }
        public void RecalculateNormals() { } public void RecalculateBounds() { }
    }
    public sealed class MeshFilter : Component { public Mesh sharedMesh { get => throw null; set { } } }
    public enum ColorSpace { Uninitialized = -1, Gamma = 0, Linear = 1 }
    public static class RenderSettings
    {
        public static Rendering.AmbientMode ambientMode { get => throw null; set { } }
        public static Color ambientSkyColor { get => throw null; set { } } public static Color ambientEquatorColor { get => throw null; set { } }
        public static Color ambientGroundColor { get => throw null; set { } } public static Light sun { get => throw null; set { } }
    }
    public enum EventType { MouseDown = 0, Repaint = 7, Layout = 8 }
    public class Event { public static Event current => throw null; public EventType type => throw null; }
    public enum TextAnchor { UpperLeft = 0, MiddleCenter = 4 }
    public enum FontStyle { Normal, Bold }
    public class RectOffset { public RectOffset(int l, int r, int t, int b) { } }
    public class GUIStyleState { public Color textColor { get => throw null; set { } } }
    public class GUIStyle
    {
        public GUIStyle(GUIStyle other) { }
        public TextAnchor alignment { get => throw null; set { } } public bool richText { get => throw null; set { } }
        public bool wordWrap { get => throw null; set { } } public int fontSize { get => throw null; set { } }
        public FontStyle fontStyle { get => throw null; set { } } public RectOffset padding { get => throw null; set { } }
        public GUIStyleState normal => throw null;
        public float CalcHeight(GUIContent c, float w) => throw null;
    }
    public class GUISkin : ScriptableObject { public GUIStyle box => throw null; public GUIStyle label => throw null; public GUIStyle button => throw null; }
    public class GUIContent { public GUIContent(string t) { } }
    public static class GUI
    {
        public static Color color { get => throw null; set { } } public static GUISkin skin => throw null;
        public static void DrawTexture(Rect r, Texture2D t) { }
        public static void Label(Rect r, string t, GUIStyle s) { }
        public static void Label(Rect r, GUIContent c, GUIStyle s) { }
        public static void Box(Rect r, GUIContent c, GUIStyle s) { }
        public static bool Button(Rect r, string text, GUIStyle s) => throw null;
    }
    public enum RuntimeInitializeLoadType { AfterSceneLoad, BeforeSceneLoad, AfterAssembliesLoaded, BeforeSplashScreen, SubsystemRegistration }
    [AttributeUsage(AttributeTargets.Method)] public class RuntimeInitializeOnLoadMethodAttribute : Attribute { public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType t) { } }
    [AttributeUsage(AttributeTargets.Class)] public class DefaultExecutionOrder : Attribute { public DefaultExecutionOrder(int o) { } }
    [AttributeUsage(AttributeTargets.Class)] public class DisallowMultipleComponent : Attribute { }
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)] public class RequireComponent : Attribute { public RequireComponent(Type t) { } public RequireComponent(Type a, Type b) { } public RequireComponent(Type a, Type b, Type c) { } }
    public enum AnimatorCullingMode { AlwaysAnimate = 0, CullUpdateTransforms = 1, CullCompletely = 2 }
    public enum AnimatorControllerParameterType { Float = 1, Int = 3, Bool = 4, Trigger = 9 }
    public class AnimatorControllerParameter
    {
        public string name { get => throw null; set { } } public AnimatorControllerParameterType type { get => throw null; set { } }
        public float defaultFloat { get => throw null; set { } } public int defaultInt { get => throw null; set { } }
    }
    public class Motion : Object { }
    public class AnimationClip : Motion { }
    public class RuntimeAnimatorController : Object { }
    public class Animator : Behaviour
    {
        public float speed { get => throw null; set { } }
        public RuntimeAnimatorController runtimeAnimatorController { get => throw null; set { } }
        public bool applyRootMotion { get => throw null; set { } }
        public AnimatorCullingMode cullingMode { get => throw null; set { } }
        public static int StringToHash(string name) => throw null;
        public void SetInteger(int id, int value) { } public void SetInteger(string name, int value) { }
        public void SetFloat(int id, float value) { } public void SetFloat(string name, float value) { }
        public int GetInteger(int id) => throw null; public int GetInteger(string name) => throw null;
    }
    public enum AudioRolloffMode { Logarithmic, Linear, Custom }
    [AttributeUsage(AttributeTargets.Field)] public class SerializeField : Attribute { }
    public class PropertyAttribute : Attribute { }
    public class HeaderAttribute : PropertyAttribute { public HeaderAttribute(string h) { } }
    public class TooltipAttribute : PropertyAttribute { public TooltipAttribute(string t) { } }
    public class RangeAttribute : PropertyAttribute { public RangeAttribute(float a, float b) { } }
    public class MinAttribute : PropertyAttribute { public MinAttribute(float m) { } }
    public class TextAreaAttribute : PropertyAttribute { public TextAreaAttribute(int a, int b) { } }
}

namespace UnityEngine.Rendering
{
    // Real SRP Core types whose names a project class could collide with (CS0104 in files that import UnityEngine.Rendering).
    public class DebugOverlay { }
    public class DebugManager { }
    public class DebugUI { }
    public class VolumeComponent : ScriptableObject { public bool active { get => throw null; set { } } }
    public class VolumeParameter<T> { public T value { get => throw null; set { } } public bool overrideState { get => throw null; set { } } public void Override(T v) { } }
    public class FloatParameter : VolumeParameter<float> { }
    public class MinFloatParameter : VolumeParameter<float> { }
    public class ClampedFloatParameter : VolumeParameter<float> { }
    public class ColorParameter : VolumeParameter<Color> { }
    public class VolumeProfile : ScriptableObject
    {
        public System.Collections.Generic.List<VolumeComponent> components => throw null;
        public T Add<T>(bool overrides = false) where T : VolumeComponent => throw null;
        public bool TryGet<T>(out T component) where T : VolumeComponent => throw null;
        public bool Has<T>() where T : VolumeComponent => throw null;
    }
    public class Volume : MonoBehaviour
    {
        public bool isGlobal { get => throw null; set { } }
        public float priority { get => throw null; set { } }
        public float weight { get => throw null; set { } }
        public VolumeProfile sharedProfile { get => throw null; set { } }
        public VolumeProfile profile { get => throw null; set { } }
    }
    public enum ShadowCastingMode { Off, On, TwoSided, ShadowsOnly }
    public enum BlendMode { Zero = 0, One = 1, DstColor = 2, SrcColor = 3, OneMinusDstColor = 4, SrcAlpha = 5, OneMinusSrcColor = 6, DstAlpha = 7, OneMinusDstAlpha = 8, SrcAlphaSaturate = 9, OneMinusSrcAlpha = 10 }
    public enum RenderQueue { Background = 1000, Geometry = 2000, AlphaTest = 2450, GeometryLast = 2500, Transparent = 3000, Overlay = 4000 }
    public enum AmbientMode { Skybox = 0, Trilight = 1, Flat = 3, Custom = 4 }
    public class RenderPipelineAsset : ScriptableObject { }
    public static class GraphicsSettings { public static RenderPipelineAsset defaultRenderPipeline { get => throw null; set { } } public static RenderPipelineAsset currentRenderPipeline => throw null; }
}

namespace UnityEngine.Rendering.Universal
{
    using UnityEngine.Rendering;
    public sealed class Bloom : VolumeComponent { public MinFloatParameter intensity, threshold; public ClampedFloatParameter scatter; }
    public sealed class Vignette : VolumeComponent { public ClampedFloatParameter intensity, smoothness; public ColorParameter color; }
    public sealed class ChromaticAberration : VolumeComponent { public ClampedFloatParameter intensity; }
    public sealed class LensDistortion : VolumeComponent { public ClampedFloatParameter intensity; }
    public enum TonemappingMode { None, Neutral, ACES }
    public sealed class TonemappingModeParameter : VolumeParameter<TonemappingMode> { }
    public sealed class Tonemapping : VolumeComponent { public TonemappingModeParameter mode; }
    public sealed class ColorAdjustments : VolumeComponent { public FloatParameter postExposure; public ClampedFloatParameter contrast, saturation; }
    public enum AntialiasingMode { None, FastApproximateAntialiasing, SubpixelMorphologicalAntiAliasing, TemporalAntiAliasing }
    public sealed class UniversalAdditionalCameraData : MonoBehaviour
    {
        public bool renderPostProcessing { get => throw null; set { } }
        public AntialiasingMode antialiasing { get => throw null; set { } }
    }
    public static class CameraExtensions { public static UniversalAdditionalCameraData GetUniversalAdditionalCameraData(this Camera c) => throw null; }
}

namespace UnityEngine.SceneManagement
{
    public struct Scene { public string name => throw null; public bool isLoaded => throw null; public bool IsValid() => throw null; public int buildIndex => throw null; public string path => throw null; public GameObject[] GetRootGameObjects() => throw null; }

    public static class SceneManager
    {
        public static Scene GetActiveScene() => throw null;
        public static Scene GetSceneByPath(string p) => throw null;
        public static void LoadScene(int i) { }
        public static void LoadScene(string sceneName) { }
        public static Scene CreateScene(string name) => throw null;
        public static bool SetActiveScene(Scene s) => throw null;
        public static UnityEngine.AsyncOperation UnloadSceneAsync(Scene s) => throw null;
    }
    public enum LoadSceneMode { Single = 0, Additive = 1 }
    public struct LoadSceneParameters { public LoadSceneParameters(LoadSceneMode mode) { } }
    public class AsyncOperationStub { }
    public static class SceneManagerUnused
    {
    }
}

namespace UnityEngine.TestTools
{
    [AttributeUsage(AttributeTargets.Method)] public class UnityTearDownAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Method)] public class UnitySetUpAttribute : Attribute { }
    public static class LogAssert { public static void Expect(UnityEngine.LogType type, System.Text.RegularExpressions.Regex message) { } public static void Expect(UnityEngine.LogType type, string message) { } }
    [AttributeUsage(AttributeTargets.Method)] public class UnityTestAttribute : Attribute { }
}

namespace UnityEngine.InputSystem
{
    public class InputDevice : InputControl { }
    public class Pointer : InputDevice { }
    public class InputControl { public InputDevice device => throw null; }
    public class ButtonControl : InputControl { public bool wasPressedThisFrame => throw null; }
    public class Keyboard : InputDevice { public static Keyboard current => throw null; public ButtonControl f1Key => throw null; public ButtonControl f5Key => throw null; }
    public struct InputBinding
    {
        public bool isComposite => throw null; public bool isPartOfComposite => throw null; public string groups => throw null;
        public string name => throw null; public string path => throw null; public string effectivePath => throw null;
        [Flags] public enum DisplayStringOptions { DontUseShortDisplayNames = 1 }
    }
    public interface IInputActionCollection2 { }
    public static class InputActionRebindingExtensions
    {
        public sealed class RebindingOperation : IDisposable
        {
            public RebindingOperation WithControlsExcluding(string path) => throw null;
            public RebindingOperation WithCancelingThrough(string binding) => throw null;
            public RebindingOperation OnMatchWaitForAnother(float seconds) => throw null;
            public RebindingOperation OnComplete(Action<RebindingOperation> callback) => throw null;
            public RebindingOperation OnCancel(Action<RebindingOperation> callback) => throw null;
            public RebindingOperation Start() => throw null;
            public void Cancel() { } public void Dispose() { }
        }
        public static RebindingOperation PerformInteractiveRebinding(this InputAction action, int bindingIndex = -1) => throw null;
        public static void RemoveAllBindingOverrides(this IInputActionCollection2 actions) { }
        public static string SaveBindingOverridesAsJson(this IInputActionCollection2 actions) => throw null;
        public static void LoadBindingOverridesFromJson(this IInputActionCollection2 actions, string json, bool removeExisting = true) { }
        public static void ApplyBindingOverride(this InputAction action, int bindingIndex, string path) { }
        public static string GetBindingDisplayString(this InputAction action, int bindingIndex, InputBinding.DisplayStringOptions options = default) => throw null;
    }
    public struct InputControlScheme { public string name => throw null; }
    public sealed class InputAction
    {
        public struct CallbackContext { }
        public event Action<CallbackContext> performed;
        public InputControl activeControl => throw null;
        public IReadOnlyList<InputBinding> bindings => throw null;
        public TValue ReadValue<TValue>() where TValue : struct => throw null;
        public bool IsPressed() => throw null;
    }
    public sealed class InputActionMap
    {
        public InputAction FindAction(string n, bool throwIfNotFound = false) => throw null;
        public void Enable() { } public void Disable() { }
    }
    public class InputActionAsset : ScriptableObject, IInputActionCollection2
    {
        public InputActionMap FindActionMap(string n, bool throwIfNotFound = false) => throw null;
        public IReadOnlyList<InputControlScheme> controlSchemes => throw null;
    }
}

namespace UnityEditor
{
    using UnityEngine;
    [AttributeUsage(AttributeTargets.Method)] public class MenuItem : Attribute { public MenuItem(string p) { } public int priority; }
    public enum SerializedPropertyType { Integer = 0, ObjectReference = 5 }
    public class SerializedProperty
    {
        public Object objectReferenceValue { get => throw null; set { } }
        public string stringValue { get => throw null; set { } } public float floatValue { get => throw null; set { } } public bool boolValue { get => throw null; set { } }
        public int intValue { get => throw null; set { } } public int arraySize { get => throw null; set { } }
        public bool isArray => throw null; public string name => throw null; public string propertyPath => throw null;
        public SerializedPropertyType propertyType => throw null; public string type => throw null;
        public SerializedProperty GetArrayElementAtIndex(int i) => throw null;
        public bool NextVisible(bool enterChildren) => throw null;
    }
    public class SerializedObject
    {
        public SerializedObject(Object o) { }
        public SerializedProperty FindProperty(string p) => throw null;
        public SerializedProperty GetIterator() => throw null;
        public bool ApplyModifiedPropertiesWithoutUndo() => throw null; public void Update() { }
    }
    public static class AssetDatabase
    {
        public static T LoadAssetAtPath<T>(string p) where T : Object => throw null;
        public static void SaveAssetIfDirty(Object o) { }
        public static bool DeleteAsset(string path) => throw null;
        public static Object[] LoadAllAssetsAtPath(string p) => throw null;
        public static void SaveAssets() { }
        public static bool IsValidFolder(string p) => throw null;
        public static string CreateFolder(string parent, string name) => throw null;
        public static void CreateAsset(Object o, string p) { }
        public static string[] FindAssets(string filter) => throw null;
        public static void ImportAsset(string path) { }
        public static void AddObjectToAsset(Object o, Object asset) { }
        public static string GUIDToAssetPath(string guid) => throw null;
        public static string GetAssetPath(Object o) => throw null;
    }
    public static class EditorUtility
    {
        public static bool DisplayDialog(string t, string m, string ok) => throw null;
        public static bool DisplayDialog(string t, string m, string ok, string cancel) => throw null;
        public static void SetDirty(Object o) { }
        public static bool IsPersistent(Object o) => throw null;
    }
    public class EditorBuildSettingsScene { public EditorBuildSettingsScene(string p, bool e) { } public string path => throw null; public bool enabled => throw null; }
    public static class EditorApplication { public static bool isPlaying { get => throw null; set { } } }
    public static class EditorBuildSettings { public static EditorBuildSettingsScene[] scenes { get => throw null; set { } } }
    public static class PlayerSettings { public static ColorSpace colorSpace { get => throw null; set { } } }
    public class AssetImporter : Object { public string assetPath => throw null; public static AssetImporter GetAtPath(string path) => throw null; }
    public enum ModelImporterMaterialImportMode { None = 0, ImportStandard = 1, ImportViaMaterialDescription = 2 }
    public enum ModelImporterAnimationType { None = 0, Legacy = 1, Generic = 2, Human = 3 }
    public class ModelImporterClipAnimation { public string name { get => throw null; set { } } public bool loopTime { get => throw null; set { } } }
    public class ModelImporter : AssetImporter
    {
        public ModelImporterMaterialImportMode materialImportMode { get => throw null; set { } }
        public bool importCameras { get => throw null; set { } } public bool importLights { get => throw null; set { } }
        public bool addCollider { get => throw null; set { } } public bool importAnimation { get => throw null; set { } }
        public ModelImporterAnimationType animationType { get => throw null; set { } }
        public ModelImporterClipAnimation[] defaultClipAnimations => throw null;
        public ModelImporterClipAnimation[] clipAnimations { get => throw null; set { } }
    }
    public class AssetPostprocessor { public string assetPath { get => throw null; set { } } public AssetImporter assetImporter => throw null; }
    public enum PrefabUnpackMode { OutermostRoot, Completely }
    public enum InteractionMode { AutomatedAction, UserAction }
    public static class PrefabUtility
    {
        public static void UnpackPrefabInstance(GameObject instanceRoot, PrefabUnpackMode unpackMode, InteractionMode action) { }
        public static GameObject SaveAsPrefabAsset(GameObject instanceRoot, string assetPath) => throw null;
        public static Object InstantiatePrefab(Object assetComponentOrGameObject, Transform parent) => throw null;
        public static void RecordPrefabInstancePropertyModifications(Object targetObject) { }
        public static bool IsPartOfPrefabInstance(Object componentOrGameObject) => throw null;
    }
    public class Editor : ScriptableObject { }
    public class EditorWindow : ScriptableObject { }
    public class MonoScript : Object { public System.Type GetClass() => throw null; }
    public static class TypeCache
    {
        public struct TypeCollection : IEnumerable<Type>
        {
            public IEnumerator<Type> GetEnumerator() => throw null;
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => throw null;
        }
        public static TypeCollection GetTypesDerivedFrom<T>() => throw null;
    }
    public static class GameObjectUtility { public static int GetMonoBehavioursWithMissingScriptCount(GameObject g) => throw null; }
}

namespace UnityEditor.SceneManagement
{
    using UnityEngine;
    using UnityEngine.SceneManagement;
    public enum NewSceneSetup { EmptyScene, DefaultGameObjects }
    public enum NewSceneMode { Single, Additive }
    public enum OpenSceneMode { Single, Additive, AdditiveWithoutLoading }
    public static class EditorSceneManager
    {
        public static Scene NewScene(NewSceneSetup s, NewSceneMode m) => throw null;
        public static Scene OpenScene(string p, OpenSceneMode m) => throw null;
        public static bool CloseScene(Scene s, bool remove) => throw null;
        public static bool SaveScene(Scene s, string p) => throw null;
        public static bool SaveCurrentModifiedScenesIfUserWantsTo() => throw null;
        public static UnityEngine.AsyncOperation LoadSceneAsyncInPlayMode(string path, LoadSceneParameters parameters) => throw null;
    }
}

namespace UnityEngine.UIElements
{
    public enum PickingMode { Position, Ignore }
    public enum DisplayStyle { Flex, None }
    public enum StyleKeyword { Undefined, Null, Auto, None, Initial }
    public struct StyleEnum<T> where T : struct, IConvertible
    {
        public T value { get => throw null; set { } }
        public static implicit operator StyleEnum<T>(T v) => throw null;
        public static implicit operator StyleEnum<T>(StyleKeyword k) => throw null;
        public static bool operator ==(StyleEnum<T> a, StyleEnum<T> b) => throw null;
        public static bool operator !=(StyleEnum<T> a, StyleEnum<T> b) => throw null;
        public override bool Equals(object o) => throw null; public override int GetHashCode() => throw null;
    }
    public struct Length { public Length(float v) { } public static Length Percent(float v) => throw null; }
    public struct StyleLength
    {
        public static implicit operator StyleLength(float v) => throw null;
        public static implicit operator StyleLength(Length v) => throw null;
        public static implicit operator StyleLength(StyleKeyword k) => throw null;
    }
    public struct StyleFloat
    {
        public static implicit operator StyleFloat(float v) => throw null;
        public static implicit operator StyleFloat(StyleKeyword k) => throw null;
    }
    public struct StyleColor
    {
        public static implicit operator StyleColor(Color v) => throw null;
        public static implicit operator StyleColor(StyleKeyword k) => throw null;
    }
    public interface IStyle
    {
        StyleEnum<DisplayStyle> display { get; set; }
        StyleLength width { get; set; } StyleLength height { get; set; }
        StyleLength left { get; set; } StyleLength top { get; set; }
        StyleFloat opacity { get; set; }
        StyleColor color { get; set; } StyleColor backgroundColor { get; set; }
        StyleColor borderBottomColor { get; set; } StyleColor borderLeftColor { get; set; }
        StyleFontDefinition unityFontDefinition { get; set; }
    }
    public struct FontDefinition { public static FontDefinition FromFont(Font f) => throw null; }
    public struct StyleFontDefinition { public StyleFontDefinition(FontDefinition v) { } }
    public struct UQueryBuilder<T> where T : VisualElement { public void ForEach(System.Action<T> funcCall) { } }
    public interface IPanel { }
    public interface IVisualElementScheduledItem { }
    public interface IVisualElementScheduler { IVisualElementScheduledItem Execute(System.Action timerUpdateEvent); }
    public struct VisualElementStyleSheetSet
    {
        public void Add(StyleSheet s) { } public bool Contains(StyleSheet s) => throw null; public bool Remove(StyleSheet s) => throw null;
    }
    public abstract class CallbackEventHandler { }
    public abstract class Focusable : CallbackEventHandler { public virtual void Focus() { } public void Blur() { } }
    public class VisualElement : Focusable
    {
        public string name { get => throw null; set { } }
        public PickingMode pickingMode { get => throw null; set { } }
        public object userData { get => throw null; set { } }
        public IStyle style => throw null;
        public VisualElementStyleSheetSet styleSheets => throw null;
        public IPanel panel => throw null;
        public VisualElement parent => throw null;
        public IVisualElementScheduler schedule => throw null;
        public int childCount => throw null;
        public VisualElement this[int index] => throw null;
        public void Add(VisualElement child) { } public void Insert(int index, VisualElement child) { }
        public void Clear() { } public void RemoveFromHierarchy() { }
        public void AddToClassList(string c) { } public void RemoveFromClassList(string c) { }
        public void EnableInClassList(string c, bool enable) { } public bool ClassListContains(string c) => throw null;
        public void SetEnabled(bool value) { } public bool enabledSelf => throw null;
    }
    public class TextElement : VisualElement { public virtual string text { get => throw null; set { } } }
    public delegate void EventCallback<in TEventType>(TEventType evt);
    public class ChangeEvent<T> { public T previousValue => throw null; public T newValue => throw null; }
    public interface INotifyValueChanged<T> { T value { get; set; } void SetValueWithoutNotify(T newValue); }
    public static class INotifyValueChangedExtensions
    {
        public static bool RegisterValueChangedCallback<T>(this INotifyValueChanged<T> control, EventCallback<ChangeEvent<T>> callback) => throw null;
    }
    public abstract class BaseField<TValueType> : VisualElement, INotifyValueChanged<TValueType>
    {
        public string label { get => throw null; set { } }
        public virtual TValueType value { get => throw null; set { } }
        public virtual void SetValueWithoutNotify(TValueType newValue) { }
    }
    public abstract class BaseSlider<TValueType> : BaseField<TValueType> { public TValueType lowValue { get => throw null; set { } } public TValueType highValue { get => throw null; set { } } }
    public class Slider : BaseSlider<float> { public Slider() { } public Slider(string label, float start = 0f, float end = 10f) { } }
    public class Toggle : BaseField<bool> { public Toggle() { } public Toggle(string label) { } }
    public class Label : TextElement { public Label() { } public Label(string text) { } }
    public class Button : TextElement { public Button() { } public Button(System.Action clickEvent) { } public event System.Action clicked; }
    public static class UQueryExtensions
    {
        public static T Q<T>(this VisualElement e, string name = null, params string[] classes) where T : VisualElement => throw null;
        public static VisualElement Q(this VisualElement e, string name = null, params string[] classes) => throw null;
        public static UQueryBuilder<VisualElement> Query(this VisualElement e, string name = null, string className = null) => throw null;
    }
    public class StyleSheet : ScriptableObject { }
    public class ThemeStyleSheet : StyleSheet { }
    public enum PanelScaleMode { ConstantPixelSize, ConstantPhysicalSize, ScaleWithScreenSize }
    public enum PanelScreenMatchMode { MatchWidthOrHeight, ShrinkToFit, ExpandToFit }
    public class PanelSettings : ScriptableObject
    {
        public ThemeStyleSheet themeStyleSheet { get => throw null; set { } }
        public PanelScaleMode scaleMode { get => throw null; set { } }
        public Vector2Int referenceResolution { get => throw null; set { } }
        public PanelScreenMatchMode screenMatchMode { get => throw null; set { } }
        public float match { get => throw null; set { } }
        public float sortingOrder { get => throw null; set { } }
    }
    public sealed class UIDocument : MonoBehaviour
    {
        public PanelSettings panelSettings { get => throw null; set { } }
        public VisualElement rootVisualElement => throw null;
        public float sortingOrder { get => throw null; set { } }
    }
    public static class RuntimePanelUtils
    {
        public static Vector2 CameraTransformWorldToPanel(IPanel panel, Vector3 worldPosition, Camera camera) => throw null;
    }
}

namespace UnityEditor.Animations
{
    using UnityEngine;
    public enum AnimatorConditionMode { If = 1, IfNot = 2, Greater = 3, Less = 4, Equals = 6, NotEqual = 7 }
    public class AnimatorStateTransition : Object
    {
        public void AddCondition(AnimatorConditionMode mode, float threshold, string parameter) { }
        public bool hasExitTime { get => throw null; set { } } public float duration { get => throw null; set { } }
        public bool canTransitionToSelf { get => throw null; set { } }
    }
    public class AnimatorState : Object
    {
        public Motion motion { get => throw null; set { } }
        public bool speedParameterActive { get => throw null; set { } } public string speedParameter { get => throw null; set { } }
    }
    public struct ChildAnimatorState { public AnimatorState state { get => throw null; set { } } }
    public class AnimatorStateMachine : Object
    {
        public ChildAnimatorState[] states { get => throw null; set { } }
        public AnimatorState AddState(string name) => throw null;
        public AnimatorStateTransition AddAnyStateTransition(AnimatorState destination) => throw null;
        public AnimatorState defaultState { get => throw null; set { } }
    }
    public class AnimatorControllerLayer { public AnimatorStateMachine stateMachine { get => throw null; set { } } }
    public class AnimatorController : RuntimeAnimatorController
    {
        public static AnimatorController CreateAnimatorControllerAtPath(string path) => throw null;
        public void AddParameter(string name, AnimatorControllerParameterType type) { }
        public AnimatorControllerParameter[] parameters { get => throw null; set { } }
        public AnimatorControllerLayer[] layers { get => throw null; set { } }
    }
}
