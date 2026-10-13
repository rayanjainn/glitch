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
        public static T Instantiate<T>(T original, Vector3 position, Quaternion rotation, Transform parent) where T : Object => throw null;
        public int GetInstanceID() => throw null;
    }
    public class YieldInstruction { }
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
        public Vector3 position { get => throw null; set { } }
        public Vector3 localPosition { get => throw null; set { } }
        public Quaternion rotation { get => throw null; set { } }
        public Quaternion localRotation { get => throw null; set { } }
        public Vector3 localScale { get => throw null; set { } }
        public Vector3 lossyScale => throw null;
        public Transform parent { get => throw null; set { } }
        public Vector3 TransformPoint(Vector3 p) => throw null;
        public void SetParent(Transform p, bool worldPositionStays) { }
        public void SetPositionAndRotation(Vector3 p, Quaternion r) { }
        public bool IsChildOf(Transform t) => throw null;
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
        public float magnitude => throw null; public float sqrMagnitude => throw null; public Vector3 normalized => throw null;
        public static Vector3 Cross(Vector3 a, Vector3 b) => throw null;
        public static Vector3 Scale(Vector3 a, Vector3 b) => throw null;
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
        public static Quaternion RotateTowards(Quaternion a, Quaternion b, float d) => throw null;
        public static Vector3 operator *(Quaternion q, Vector3 v) => throw null;
        public static Quaternion operator *(Quaternion a, Quaternion b) => throw null;
    }
    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b) { this.r = r; this.g = g; this.b = b; a = 1; }
        public Color(float r, float g, float b, float a) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static Color white => default; public static Color blue => default; public static Color magenta => default;
        public static Color operator *(Color c, float f) => throw null;
    }
    public struct Rect { public Rect(float x, float y, float w, float h) { } }
    public struct Bounds
    {
        public Bounds(Vector3 c, Vector3 s) { }
        public Vector3 center => throw null; public Vector3 size => throw null; public Vector3 extents => throw null; public Vector3 min => throw null; public Vector3 max => throw null;
        public void Encapsulate(Bounds b) { } public void Expand(float a) { }
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
        public static bool SphereCast(Vector3 o, float r, Vector3 d, out RaycastHit h, float max, int mask, QueryTriggerInteraction q) => throw null;
        public static int SphereCastNonAlloc(Ray ray, float r, RaycastHit[] results, float max, int mask, QueryTriggerInteraction q) => throw null;
        public static void IgnoreLayerCollision(int a, int b, bool ignore) { }
        public static bool CheckBox(Vector3 center, Vector3 halfExtents, Quaternion orientation, int mask, QueryTriggerInteraction q) => throw null;
        public static int OverlapBoxNonAlloc(Vector3 center, Vector3 halfExtents, Collider[] results, Quaternion orientation, int mask, QueryTriggerInteraction q) => throw null;
        public static bool GetIgnoreLayerCollision(int a, int b) => throw null;
    }
    public static class Mathf
    {
        public const float PI = 3.14159265f;
        public static float Max(float a, float b) => throw null; public static int Max(int a, int b) => throw null;
        public static float Clamp(float v, float a, float b) => throw null;
        public static float Abs(float v) => throw null; public static float Sin(float v) => throw null; public static float Cos(float v) => throw null;
        public static float Repeat(float t, float l) => throw null; public static float MoveTowards(float a, float b, float d) => throw null;
        public static float Lerp(float a, float b, float t) => throw null;
        public static int RoundToInt(float v) => throw null; public static int CeilToInt(float v) => throw null;
    }
    public static class Time { public static float fixedDeltaTime => throw null; public static float deltaTime => throw null; public static float unscaledDeltaTime => throw null; public static float time => throw null; public static float unscaledTime => throw null; public static float timeScale { get => throw null; set { } } }
    public enum CursorLockMode { None, Locked, Confined }
    public static class Cursor { public static CursorLockMode lockState { get => throw null; set { } } public static bool visible { get => throw null; set { } } }
    public static class Screen { public static int width => throw null; public static int height => throw null; }
    public static class Application { public static string unityVersion => throw null; public static bool isPlaying => throw null; }
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
    public class TrailRenderer : Renderer { }
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
    }
    public enum CameraClearFlags { Skybox = 1, SolidColor = 2, Depth = 3, Nothing = 4 }
    public class AudioListener : Behaviour { }
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
    public class Texture2D : Object { public static Texture2D whiteTexture => throw null; }
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
    public class GUISkin : ScriptableObject { public GUIStyle box => throw null; public GUIStyle label => throw null; }
    public class GUIContent { public GUIContent(string t) { } }
    public static class GUI
    {
        public static Color color { get => throw null; set { } } public static GUISkin skin => throw null;
        public static void DrawTexture(Rect r, Texture2D t) { }
        public static void Label(Rect r, string t, GUIStyle s) { }
        public static void Box(Rect r, GUIContent c, GUIStyle s) { }
    }
    public enum RuntimeInitializeLoadType { AfterSceneLoad, BeforeSceneLoad, AfterAssembliesLoaded, BeforeSplashScreen, SubsystemRegistration }
    [AttributeUsage(AttributeTargets.Method)] public class RuntimeInitializeOnLoadMethodAttribute : Attribute { public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType t) { } }
    [AttributeUsage(AttributeTargets.Class)] public class DefaultExecutionOrder : Attribute { public DefaultExecutionOrder(int o) { } }
    [AttributeUsage(AttributeTargets.Class)] public class DisallowMultipleComponent : Attribute { }
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)] public class RequireComponent : Attribute { public RequireComponent(Type t) { } public RequireComponent(Type a, Type b) { } public RequireComponent(Type a, Type b, Type c) { } }
    public class Animator : Behaviour { public float speed { get => throw null; set { } } }
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
    public enum ShadowCastingMode { Off, On, TwoSided, ShadowsOnly }
    public enum BlendMode { Zero = 0, One = 1, DstColor = 2, SrcColor = 3, OneMinusDstColor = 4, SrcAlpha = 5, OneMinusSrcColor = 6, DstAlpha = 7, OneMinusDstAlpha = 8, SrcAlphaSaturate = 9, OneMinusSrcAlpha = 10 }
    public enum RenderQueue { Background = 1000, Geometry = 2000, AlphaTest = 2450, GeometryLast = 2500, Transparent = 3000, Overlay = 4000 }
    public enum AmbientMode { Skybox = 0, Trilight = 1, Flat = 3, Custom = 4 }
    public class RenderPipelineAsset : ScriptableObject { }
    public static class GraphicsSettings { public static RenderPipelineAsset defaultRenderPipeline { get => throw null; set { } } public static RenderPipelineAsset currentRenderPipeline => throw null; }
}

namespace UnityEngine.SceneManagement
{
    public struct Scene { public bool isLoaded => throw null; public bool IsValid() => throw null; public int buildIndex => throw null; public string path => throw null; public GameObject[] GetRootGameObjects() => throw null; }

    public static class SceneManager
    {
        public static Scene GetActiveScene() => throw null;
        public static Scene GetSceneByPath(string p) => throw null;
        public static void LoadScene(int i) { }
    }
}

namespace UnityEngine.TestTools
{
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
    public struct InputBinding { public bool isComposite => throw null; public string groups => throw null; }
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
    public class InputActionAsset : ScriptableObject
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
        public string stringValue { get => throw null; set { } } public float floatValue { get => throw null; set { } }
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
        public static Object[] LoadAllAssetsAtPath(string p) => throw null;
        public static void SaveAssets() { }
        public static bool IsValidFolder(string p) => throw null;
        public static string CreateFolder(string parent, string name) => throw null;
        public static void CreateAsset(Object o, string p) { }
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
    public static class EditorBuildSettings { public static EditorBuildSettingsScene[] scenes { get => throw null; set { } } }
    public static class PlayerSettings { public static ColorSpace colorSpace { get => throw null; set { } } }
    public static class PrefabUtility
    {
        public static GameObject SaveAsPrefabAsset(GameObject instanceRoot, string assetPath) => throw null;
        public static Object InstantiatePrefab(Object assetComponentOrGameObject, Transform parent) => throw null;
        public static void RecordPrefabInstancePropertyModifications(Object targetObject) { }
        public static bool IsPartOfPrefabInstance(Object componentOrGameObject) => throw null;
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
    }
}
