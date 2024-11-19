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
    public class ReferenceFixer : UnityEditor.EditorWindow
    {

        const string Title = "Property Manager";

        [MenuItem("Simulator/" + Title)]
        public static void ShowWindow() => EditorWindow.GetWindow<ReferenceFixer>(Title);


#if true

        private Type? _TargetType;
        internal Type? TargetType {
            get => _TargetType;
            set {
                if(_TargetType != value) {
                    _TargetType = value;
                    this.Reload();
                }
            }
        }
        internal PropertyInfo? TargetProperty { get; set; }

        public enum OperationMode
        {
            Set,
            Replace,
        }

        internal OperationMode Mode { get; set; } = OperationMode.Set;

        internal UnityEngine.Object? FindValue { get; set; }

        internal UnityEngine.Object? SetValue { get; set; }

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

            if(_TargetInstances.Count < 1) return;

            foreach(var instance in _TargetInstances) {
                try {
                    if(this.Mode == OperationMode.Set) {

                    }
                    else if(this.Mode == OperationMode.Replace) {
                        if(object.Equals(this.TargetProperty.GetValue(instance), this.FindValue)) {
                            continue;
                        }
                    }
                    else {
                        throw new NotSupportedException(this.Mode.ToString());
                    }
                    this.TargetProperty.SetValue(instance, this.SetValue);
                    Debug.Log($"Set a new reference to the target property \"{this.TargetProperty.Name}\".");
                }
                catch(Exception ex) {
                    Debug.LogError(ex, instance);
                }
            }
        }


        private ISet<UnityEngine.Object> _TargetInstances = new HashSet<UnityEngine.Object>();

        private void Reload() {
            _TargetInstances.Clear();
            foreach(var found in GameObject.FindObjectsByType(TargetType, FindObjectsSortMode.None)) {
                _TargetInstances.Add(found);
            }
        }

        private void OnGUI() {
            Space();
            using(var check = new ChangeCheckScope()) {
                string typeName = DelayedTextField(new GUIContent("Target Type"), this.TargetType?.FullName ?? string.Empty);
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
                UnityEngine.Object? sample = ObjectField(new GUIContent("Sample"), null, typeof(UnityEngine.Object), allowSceneObjects: true);
                if(sample != null) {
                    TargetType = sample.GetType();
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
                    SetValue = ObjectField(new GUIContent("Replace"), SetValue);
                }
                Space();
                using(new DisabledScope(!IsValid))
                    if(GUILayout.Button(new GUIContent("Execute"))) {
                        Execute();
                    }
            }
            Space(4);
            using(new DisabledScope(true)) {
                foreach(var item in _TargetInstances) {
                    ObjectField(new GUIContent($"{item.name}"), item);
                }
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
