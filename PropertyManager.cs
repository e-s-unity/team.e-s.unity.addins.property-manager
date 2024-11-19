#nullable enable
#pragma warning disable

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using System.Reflection;

#if UNITY_EDITOR
using UnityEditor;
using static UnityEditor.EditorGUI;
using static UnityEditor.EditorGUILayout;
using static UnityEditor.EditorGUIUtility;

namespace Es.Unity.Addins
{

    /// <summary>
    /// 
    /// </summary>
    public class PropertyManager : UnityEditor.EditorWindow
    {

        const string Title = "Property Manager";

        [MenuItem("Window/" + Title)]
        public static void ShowWindow() => EditorWindow.GetWindow<PropertyManager>(Title);


#if true

        [SerializeField] private string? _SerializedTargetTypeName;
        [SerializeField] private string? _SerializedTargetPropertyName;

        private Type? _TargetType;
        internal Type? TargetType {
            get {
                if(_TargetType is null && _SerializedTargetTypeName != null) {

                    _TargetType = SampleValue?.GetType();

                    if(_TargetType is null) {
                        try {
                            _TargetType = Type.GetType(_SerializedTargetTypeName);
                        }
                        catch {
                            
                        }
                    }
                }
                return _TargetType;
            }
            set {
                if(_TargetType != value) {
                    _TargetType = value;
                    _SerializedTargetTypeName = _TargetType.FullName;
                    this.Reload();
                }
            }
        }

        private PropertyInfo? _TargetProperty;
        internal PropertyInfo? TargetProperty {
            get {
                if(_TargetProperty is null && _SerializedTargetPropertyName != null) {
                    _TargetProperty = this.TargetType?.GetProperty(_SerializedTargetPropertyName);
                }
                return _TargetProperty;
            }
            set {
                if(_TargetProperty != value) {
                    _TargetProperty = value;
                    _SerializedTargetPropertyName = _TargetProperty.Name;
                }
            }
        }

        public enum OperationMode
        {
            Set,
            Replace,
        }

        internal OperationMode Mode { get; set; } = OperationMode.Set;

        internal UnityEngine.Object? FindValue { get; set; }

        internal UnityEngine.Object? SetValue { get; set; }


        private UnityEngine.Object? SampleValue { get; set; } = null;

#else

    [SerializeField] private string? _TargetTypeName;

    private Type? _TargetType;
    public Type? TargetType {
        get {
            if(_TargetType is null) {
                if(_TargetTypeName != null) {
                    _TargetType = Type.GetType(_TargetTypeName);
                }
                else {
                    _TargetType = null;
                }
            }
            return _TargetType;
        }
        set {
            _TargetType = value;
            if(_TargetType != null) {
                _TargetTypeName = _TargetType.FullName;
            }
            else {
                _TargetTypeName = null;
            }
            
        }
    }

    [SerializeField] private string? _TargetPropertyName;

    public PropertyInfo? TargetProperty {
        get {
            if(_TargetType is null) return null;
            if(_TargetPropertyName != null) {
                return _TargetType.GetProperty(_TargetPropertyName);
            }
            else {
                return null;
            }
        }
        set {
            if(value is null) {
                _TargetPropertyName = null;
                return;
            }
            if(this.TargetType != null) {
                if(value.DeclaringType == this.TargetType || value.DeclaringType.IsSubclassOf(this.TargetType)) {
                    _TargetPropertyName = value.Name;
                }
            }
        }
    }

    public void SetTypeBySample(UnityEngine.Object instance) {
        this.TargetType = instance.GetType();
    }

#endif

        public bool IsComponent => this.TargetType?.IsSubclassOf(typeof(UnityEngine.Component)) ?? false;
        public bool IsAsset => this.TargetType?.IsSubclassOf(typeof(UnityEngine.ScriptableObject)) ?? false;

        public bool IsValid {
            get {
                if(TargetType != null && TargetProperty != null) {
                    if(TargetProperty.DeclaringType == TargetType || TargetProperty.DeclaringType.IsSubclassOf(TargetType)) {
                        return true;
                    }
                }
                return false;
            }
        }

        public void Execute() {
            if(this.TargetType == null || this.TargetProperty == null) throw new InvalidOperationException();

            if(_TargetInstances.Count < 1) this.Reload();

            if(FilteredTargetInstances.Count() < 1) return;

            foreach(var instance in FilteredTargetInstances) {
                try {
                    this.TargetProperty.SetValue(instance, this.SetValue);
                    Debug.Log($"Set a new reference to the target property \"{this.TargetProperty.Name}\".");
                }
                catch(Exception ex) {
                    Debug.LogError(ex, instance);
                }
            }
        }


        private ISet<UnityEngine.Object> _TargetInstances = new HashSet<UnityEngine.Object>();

        internal IEnumerable<UnityEngine.Object> FilteredTargetInstances {
            get {
                if(this.Mode == OperationMode.Set) return _TargetInstances;
                else if(this.Mode == OperationMode.Replace) {
                    return _TargetInstances?.Where(_ => CustomEquals(this.TargetProperty?.GetValue(_) as UnityEngine.Object, this.FindValue)) ?? Enumerable.Empty<UnityEngine.Object>();
                }
                return _TargetInstances;
            }
        }

        private static bool CustomEquals(UnityEngine.Object? a, UnityEngine.Object? b) {
            if(a == null) a = null;
            if(b == null) b = null;
            if(a is null) return b is null;
            else {
                return a.Equals(b);
            }
        }

        private void Reload() {
            _TargetInstances.Clear();
            foreach(var found in GameObject.FindObjectsByType(TargetType, FindObjectsSortMode.None)) {
                _TargetInstances.Add(found);
            }
        }

        private static GUIStyle? __CenteredLabelStyle;
        private static GUIStyle _CenteredLabelStyle => __CenteredLabelStyle ??= new(EditorStyles.label) { alignment = TextAnchor.MiddleCenter };

        private void OnGUI() {
            Space();
            using(var check = new ChangeCheckScope()) {
                string typeName = DelayedTextField(new GUIContent("Target Type", "The fully name of the target type."), this.TargetType?.FullName ?? string.Empty);
                if(check.changed) {
                    try {
                        Type type = Type.GetType(typeName);
                        this.TargetType = type;
                    }
                    catch {

                    }
                }
            }
            using(new IndentLevelScope()) {
                using(var check = new ChangeCheckScope()) {
                    SampleValue = ObjectField(new GUIContent("From Sample"), SampleValue);
                    if(check.changed) {
                        if(SampleValue != null) {
                            TargetType = SampleValue.GetType();
                        }
                    }
                }
            }
            TargetProperty = PropertyField(new GUIContent("Target Property"), TargetProperty, TargetType);
            if(TargetType != null && TargetProperty != null) {
                if(TargetProperty.DeclaringType != TargetType && !TargetProperty.DeclaringType.IsSubclassOf(TargetType)) {
                    HelpBox("Invalid Member" + "\r\n" + $"The target type \"{TargetType.FullName}\" definition not contains the specified property \"{TargetProperty.Name}\".", MessageType.Error);
                }
            }
            Space();
            Space();
            Mode = (OperationMode)EnumPopup(new GUIContent("Mode"), Mode);
            using(new IndentLevelScope()) {
                if(Mode == OperationMode.Set) {
                    SetValue = ObjectField(new GUIContent("Set", "The value to set."), SetValue);
                }
                else if(Mode == OperationMode.Replace) {
                    FindValue = ObjectField(new GUIContent("Find"), FindValue);
                    using(new HorizontalScope()) {
                        PrefixLabel("　");
                        if(GUILayout.Button(new GUIContent("⇅", "Swap"), _CenteredLabelStyle)) {
                            (FindValue, SetValue) = (SetValue, FindValue);
                        }
                    }
                    SetValue = ObjectField(new GUIContent("Replace"), SetValue);
                }
                Space();
                using(new DisabledScope(!IsValid)) {
                    if(GUILayout.Button(new GUIContent("Execute"))) {
                        Execute();
                    }
                }
            }
            Space(4);
            var instances = FilteredTargetInstances;
            using(new HorizontalScope()) {
                using(new DisabledScope(true)) {
                    IntField(new GUIContent("Match"), instances.Count(), GUILayout.Width(labelWidth + 50));
                }
                if(GUILayout.Button("Reload")) {
                    this.Reload();
                }
            }
            if(instances.Any()) {
                using(new DisabledScope(true)) {
                    using(new IndentLevelScope()) {
                        foreach(var item in instances) {
                            ObjectField(new GUIContent($"{item.name}"), item);
                        }
                    }
                }
            }
            else {
                HelpBox("There is no matched items.", MessageType.Info);
            }
        }


        public static UnityEngine.Object? ObjectField(GUIContent content, UnityEngine.Object? current, Type? typeFilter = null, bool allowSceneObjects = true, params GUILayoutOption[] options) {
            typeFilter ??= typeof(UnityEngine.Object);
            current = EditorGUILayout.ObjectField(content, current, typeFilter, allowSceneObjects: true, options);
            return current;
        }

        public static PropertyInfo? PropertyField(GUIContent content, PropertyInfo? current, Type? type = null, BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance, params GUILayoutOption[] options) {
            if(type == null) return null;
            if(current != null) type ??= current.DeclaringType;
            var properties = type.GetProperties(bindingFlags);
            GUIContent[] contents = properties.Select(_ => new GUIContent(_.Name, _.PropertyType.Name)).ToArray();
            int currentIndex = Array.IndexOf(properties, current);

            currentIndex = Popup(content, currentIndex, contents, options);

            if(-1 < currentIndex && currentIndex < properties.Length) {
                return properties[currentIndex];
            }
            else {
                return null;
            }
        }

    }

}

#endif
